// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
// 
// #include this cpp file to get the definition and implementation of the ILRewriter
// class.  This class contains a lot of general-purpose IL rewriting functionality,
// which parses an IL stream into a structured linked list of IL instructions.  This
// list can then be manipulated (items added / removed), and then rewritten back into an
// IL stream, with things like branches automatically updated.
// 
// Refer to the C functions at the bottom of this file for examples of how the
// ILRewriter class can be used
// 


//#include "profilermain.h"
#pragma warning( disable : 4091 )
#include <corhlpr.cpp>
#include <cor.h>
#include <corprof.h>
#pragma warning( default : 4091 )
#include "sigparse.inl"

#include <assert.h>

#include <map>
#include <unordered_map>
#include <stack>
#include <array>
#include <string>
#include <sstream>
#include <vector>
#include <queue>
#include <stdexcept>
#include <unordered_set>
#include <locale>
#include <memory>
#include <allocators>

// ILRewriter::Export intentionally does a comparison by casting a variable (delta) down
// to an INT8, with data loss being expected and handled. This pragma is required because
// this is compiled with RTC on, and without the pragma, the above cast will generate a
// run-time check on whether we lose data, and cause an unhandled exception (look up
// RTC_Check_4_to_1).  In theory, I should be able to just bracket the Export function
// with the #pragma, but that didn't work.  (Perhaps because all the functions are
// defined inline in the class definition?)
#pragma runtime_checks("", off)

void __fastcall UnmanagedInspectObject(void* pv)
{
	void* pv2 = pv;
}

#undef IfFailRet
#define IfFailRet(EXPR) do { HRESULT hr = (EXPR); if(FAILED(hr)) { return (hr); } } while (0)

#undef IfNullRet
#define IfNullRet(EXPR) do { if ((EXPR) == NULL) return E_OUTOFMEMORY; } while (0)

struct ILInstr
{
	ILInstr *       m_pNext;
	ILInstr *       m_pPrev;

	unsigned        m_opcode;
	unsigned        m_offset;

	union
	{
		ILInstr *   m_pTarget;
		INT8        m_Arg8;
		INT16       m_Arg16;
		INT32       m_Arg32;
		INT64       m_Arg64;
	};
};

struct EHClause
{
	CorExceptionFlag            m_Flags;
	ILInstr *                   m_pTryBegin;
	ILInstr *                   m_pTryEnd;
	ILInstr *                   m_pHandlerBegin;    // First instruction inside the handler
	ILInstr *                   m_pHandlerEnd;      // Last instruction inside the handler
	union
	{
		DWORD                   m_ClassToken;   // use for type-based exception handlers
		ILInstr *               m_pFilter;      // use for filter-based exception handlers (COR_ILEXCEPTION_CLAUSE_FILTER is set)
	};
};

typedef enum
{
#define OPDEF(c,s,pop,push,args,type,l,s1,s2,ctrl) c,
#include "opcode.def"
#undef OPDEF
	CEE_COUNT,
	CEE_SWITCH_ARG, // special internal instructions
} OPCODE;

#define OPCODEFLAGS_SizeMask        0x0F
#define OPCODEFLAGS_BranchTarget    0x10
#define OPCODEFLAGS_Switch          0x20

static const BYTE s_OpCodeFlags[] =
{
#define InlineNone           0
#define ShortInlineVar       1
#define InlineVar            2
#define ShortInlineI         1
#define InlineI              4
#define InlineI8             8
#define ShortInlineR         4
#define InlineR              8
#define ShortInlineBrTarget  1 | OPCODEFLAGS_BranchTarget
#define InlineBrTarget       4 | OPCODEFLAGS_BranchTarget
#define InlineMethod         4
#define InlineField          4
#define InlineType           4
#define InlineString         4
#define InlineSig            4
#define InlineRVA            4
#define InlineTok            4
#define InlineSwitch         0 | OPCODEFLAGS_Switch

#define OPDEF(c,s,pop,push,args,type,l,s1,s2,flow) args,
#include "opcode.def"
#undef OPDEF

#undef InlineNone
#undef ShortInlineVar
#undef InlineVar
#undef ShortInlineI
#undef InlineI
#undef InlineI8
#undef ShortInlineR
#undef InlineR
#undef ShortInlineBrTarget
#undef InlineBrTarget
#undef InlineMethod
#undef InlineField
#undef InlineType
#undef InlineString
#undef InlineSig
#undef InlineRVA
#undef InlineTok
#undef InlineSwitch
	0,                              // CEE_COUNT
	4 | OPCODEFLAGS_BranchTarget,   // CEE_SWITCH_ARG
};

static int k_rgnStackPushes[] = {

#define OPDEF(c,s,pop,push,args,type,l,s1,s2,ctrl) \
	{ push },

#define Push0    0
#define Push1    1
#define PushI    1
#define PushI4   1
#define PushR4   1
#define PushI8   1
#define PushR8   1
#define PushRef  1
#define VarPush  1          // Test code doesn't call vararg fcns, so this should not be used

#include "opcode.def"

#undef Push0   
#undef Push1   
#undef PushI   
#undef PushI4  
#undef PushR4  
#undef PushI8  
#undef PushR8  
#undef PushRef 
#undef VarPush 
#undef OPDEF
};

class ILRewriter
{
private:
	ICorProfilerInfo * m_pICorProfilerInfo;
	ICorProfilerFunctionControl * m_pICorProfilerFunctionControl;

	ModuleID    m_moduleId;
	mdToken     m_tkMethod;

	mdToken     m_tkLocalVarSig;
	unsigned    m_maxStack;
	unsigned    m_flags;
	bool        m_fGenerateTinyHeader;

	ILInstr m_IL; // Double linked list of all il instructions

	unsigned    m_nEH;
	EHClause *  m_pEH;

	// Helper table for importing.  Sparse array that maps BYTE offset of beginning of an
	// instruction to that instruction's ILInstr*.  BYTE offsets that don't correspond
	// to the beginning of an instruction are mapped to NULL.
	ILInstr **  m_pOffsetToInstr;
	unsigned    m_CodeSize;

	unsigned    m_nInstrs;

	BYTE *      m_pOutputBuffer;

	IMethodMalloc * m_pIMethodMalloc;

	IMetaDataImport * m_pMetaDataImport;
	IMetaDataEmit * m_pMetaDataEmit;

public:
	ILRewriter(ICorProfilerInfo * pICorProfilerInfo, ICorProfilerFunctionControl * pICorProfilerFunctionControl, ModuleID moduleID, mdToken tkMethod)
		: m_pICorProfilerInfo(pICorProfilerInfo), m_pICorProfilerFunctionControl(pICorProfilerFunctionControl),
		m_moduleId(moduleID), m_tkMethod(tkMethod),m_fGenerateTinyHeader(false),
		m_pEH(nullptr), m_pOffsetToInstr(nullptr), m_pOutputBuffer(nullptr), m_pIMethodMalloc(nullptr), 
		m_pMetaDataImport(nullptr), m_pMetaDataEmit(nullptr)
	{
		m_IL.m_pNext = &m_IL;
		m_IL.m_pPrev = &m_IL;
		m_nInstrs = 0;        
	}

	~ILRewriter()
	{
		auto p = m_IL.m_pNext;
		while (p != &m_IL)
		{
			auto t = p->m_pNext;
			delete p;
			p = t;
		}
		delete [] m_pEH;
		delete [] m_pOffsetToInstr;
		delete [] m_pOutputBuffer;

		if (m_pIMethodMalloc)
			m_pIMethodMalloc->Release();
		if (m_pMetaDataImport)
			m_pMetaDataImport->Release();
		if (m_pMetaDataEmit)
			m_pMetaDataEmit->Release();
	}

	HRESULT Initialize()
	{
		// Get metadata interfaces ready

		IfFailRet(m_pICorProfilerInfo->GetModuleMetaData(
			m_moduleId, ofRead | ofWrite, IID_IMetaDataImport, reinterpret_cast<IUnknown**>(&m_pMetaDataImport)));

		IfFailRet(m_pMetaDataImport->QueryInterface(IID_IMetaDataEmit, reinterpret_cast<void **>(&m_pMetaDataEmit)));

		return S_OK;
	}

	void InitializeTiny()
	{
		m_tkLocalVarSig = 0;
		m_maxStack = 8;
		m_flags = CorILMethod_TinyFormat;
		m_CodeSize = 0;
		m_nEH = 0;
		m_fGenerateTinyHeader = true;
	}

	/////////////////////////////////////////////////////////////////////////////////////////////////
	//
	// I M P O R T
	//
	////////////////////////////////////////////////////////////////////////////////////////////////

	HRESULT Import()
	{
		LPCBYTE pMethodBytes;

		IfFailRet(m_pICorProfilerInfo->GetILFunctionBody(
			m_moduleId, m_tkMethod, &pMethodBytes, NULL));

		COR_ILMETHOD_DECODER decoder((COR_ILMETHOD*)pMethodBytes);

		// Import the header flags
		m_tkLocalVarSig = decoder.GetLocalVarSigTok();
		m_maxStack = decoder.GetMaxStack();
		m_flags = (decoder.GetFlags() & CorILMethod_InitLocals);

		m_CodeSize = decoder.GetCodeSize();

		IfFailRet(ImportIL(decoder.Code));

		IfFailRet(ImportEH(decoder.EH, decoder.EHCount()));

		return S_OK;
	}

	HRESULT ImportIL(LPCBYTE pIL)
	{
		m_pOffsetToInstr = new ILInstr* [m_CodeSize+1];
		IfNullRet(m_pOffsetToInstr);

		ZeroMemory(m_pOffsetToInstr, m_CodeSize * sizeof(ILInstr*));

		// Set the sentinel instruction
		m_pOffsetToInstr[m_CodeSize] = &m_IL;
		m_IL.m_opcode = -1;

		auto fBranch = false;
		unsigned offset = 0;
		while (offset < m_CodeSize)
		{
			auto startOffset = offset;
			unsigned opcode = pIL[offset++];

			if (opcode == CEE_PREFIX1)
			{
				if (offset >= m_CodeSize)
				{
					assert(false);
					return COR_E_INVALIDPROGRAM;
				}
				opcode = 0x100 + pIL[offset++];
			}

			if ((CEE_PREFIX7 <= opcode) && (opcode <= CEE_PREFIX2))
			{
				// NOTE: CEE_PREFIX2-7 are currently not supported
				assert(false);
				return COR_E_INVALIDPROGRAM;
			}

			if (opcode >= CEE_COUNT)
			{
				assert(false);
				return COR_E_INVALIDPROGRAM;
			}

			auto flags = s_OpCodeFlags[opcode];

			auto size = (flags & OPCODEFLAGS_SizeMask);
			if (offset + size > m_CodeSize)
			{
				assert(false);
				return COR_E_INVALIDPROGRAM;
			}

			auto pInstr = NewILInstr();
			IfNullRet(pInstr);

			pInstr->m_opcode = opcode;

			InsertBefore(&m_IL, pInstr);

			m_pOffsetToInstr[startOffset] = pInstr;

			switch (flags)
			{
			case 0:
				break;
			case 1:
				pInstr->m_Arg8 = *(UNALIGNED INT8 *)&(pIL[offset]);
				break;
			case 2:
				pInstr->m_Arg16 = *(UNALIGNED INT16 *)&(pIL[offset]);
				break;
			case 4:
				pInstr->m_Arg32 = *(UNALIGNED INT32 *)&(pIL[offset]);
				break;
			case 8:
				pInstr->m_Arg64 = *(UNALIGNED INT64 *)&(pIL[offset]);
				break;
			case 1 | OPCODEFLAGS_BranchTarget:
				pInstr->m_Arg32 = offset + 1 + *(UNALIGNED INT8 *)&(pIL[offset]);
				fBranch = true;
				break;
			case 4 | OPCODEFLAGS_BranchTarget:
				pInstr->m_Arg32 = offset + 4 + *(UNALIGNED INT32 *)&(pIL[offset]);
				fBranch = true;
				break;
			case 0 | OPCODEFLAGS_Switch:
				{
					if (offset + sizeof(INT32) > m_CodeSize)
					{
						assert(false);
						return COR_E_INVALIDPROGRAM;
					}

					unsigned nTargets = *(UNALIGNED INT32 *)&(pIL[offset]);
					pInstr->m_Arg32 = nTargets;
					offset += sizeof(INT32);

					auto base = offset + nTargets * sizeof(INT32);

					for (unsigned iTarget = 0; iTarget < nTargets; iTarget++)
					{
						if (offset + sizeof(INT32) > m_CodeSize)
						{
							assert(false);
							return COR_E_INVALIDPROGRAM;
						}

						pInstr = NewILInstr();
						IfNullRet(pInstr);

						pInstr->m_opcode = CEE_SWITCH_ARG;

						pInstr->m_Arg32 = base + *(UNALIGNED INT32 *)&(pIL[offset]);
						offset += sizeof(INT32);

						InsertBefore(&m_IL, pInstr);
					}
					fBranch = true;
					break;
				}
			default:
				assert(false);
				break;
			}
			offset += size;
		}
		assert(offset == m_CodeSize);

		if (fBranch)
		{
			// Go over all control flow instructions and resolve the targets
			for (auto pInstr = m_IL.m_pNext; pInstr != &m_IL; pInstr = pInstr->m_pNext)
			{
				if (s_OpCodeFlags[pInstr->m_opcode] & OPCODEFLAGS_BranchTarget)
					pInstr->m_pTarget = GetInstrFromOffset(pInstr->m_Arg32);
			}
		}

		return S_OK;
	}

	HRESULT ImportEH(const COR_ILMETHOD_SECT_EH* pILEH, unsigned nEH)
	{
		assert(m_pEH == NULL);

		m_nEH = nEH;

		if (nEH == 0)
			return S_OK;

		IfNullRet(m_pEH = new EHClause[m_nEH]);
		for (unsigned iEH = 0; iEH < m_nEH; iEH++)
		{
			// If the EH clause is in tiny form, the call to pILEH->EHClause() below will
			// use this as a scratch buffer to expand the EH clause into its fat form.
			COR_ILMETHOD_SECT_EH_CLAUSE_FAT scratch;

			const COR_ILMETHOD_SECT_EH_CLAUSE_FAT* ehInfo;
			ehInfo = (COR_ILMETHOD_SECT_EH_CLAUSE_FAT*)pILEH->EHClause(iEH, &scratch);

			auto clause = &(m_pEH[iEH]);
			clause->m_Flags = ehInfo->GetFlags();

			clause->m_pTryBegin = GetInstrFromOffset(ehInfo->GetTryOffset());
			clause->m_pTryEnd = GetInstrFromOffset(ehInfo->GetTryOffset() + ehInfo->GetTryLength());
			clause->m_pHandlerBegin = GetInstrFromOffset(ehInfo->GetHandlerOffset());
			clause->m_pHandlerEnd = GetInstrFromOffset(ehInfo->GetHandlerOffset() + ehInfo->GetHandlerLength())->m_pPrev;
			if ((clause->m_Flags & COR_ILEXCEPTION_CLAUSE_FILTER) == 0)
				clause->m_ClassToken = ehInfo->GetClassToken();
			else
				clause->m_pFilter = GetInstrFromOffset(ehInfo->GetFilterOffset());
		}

		return S_OK;
	}

	ILInstr* NewILInstr()
	{
		m_nInstrs++;
		return new ILInstr();
	}

	ILInstr* GetInstrFromOffset(unsigned offset)
	{
		ILInstr * pInstr = nullptr;

		if (offset <= m_CodeSize)
			pInstr = m_pOffsetToInstr[offset];

		assert(pInstr != NULL);
		return pInstr;
	}

	void InsertBefore(ILInstr * pWhere, ILInstr * pWhat)
	{
		pWhat->m_pNext = pWhere;
		pWhat->m_pPrev = pWhere->m_pPrev;

		pWhat->m_pNext->m_pPrev = pWhat;
		pWhat->m_pPrev->m_pNext = pWhat;

		AdjustState(pWhat);
	}

	void InsertAfter(ILInstr * pWhere, ILInstr * pWhat)
	{
		pWhat->m_pNext = pWhere->m_pNext;
		pWhat->m_pPrev = pWhere;

		pWhat->m_pNext->m_pPrev = pWhat;
		pWhat->m_pPrev->m_pNext = pWhat;

		AdjustState(pWhat);
	}

	void AdjustState(ILInstr * pNewInstr)
	{
		m_maxStack += k_rgnStackPushes[pNewInstr->m_opcode];
	}


	ILInstr * GetILList()
	{
		return &m_IL;
	}

	/////////////////////////////////////////////////////////////////////////////////////////////////
	//
	// E X P O R T
	//
	////////////////////////////////////////////////////////////////////////////////////////////////


	HRESULT Export()
	{
		// One instruction produces 6 bytes in the worst case
		auto maxSize = m_nInstrs * 6;

		m_pOutputBuffer = new BYTE[maxSize];
		IfNullRet(m_pOutputBuffer);

again:
	auto pIL = m_pOutputBuffer;

		auto fBranch = false;
		unsigned offset = 0;

		// Go over all instructions and produce code for them
		for (auto pInstr = m_IL.m_pNext; pInstr != &m_IL; pInstr = pInstr->m_pNext)
		{
			pInstr->m_offset = offset;

			auto opcode = pInstr->m_opcode;
			if (opcode < CEE_COUNT)
			{
				// CEE_PREFIX1 refers not to instruction prefixes (like tail.), but to
				// the lead byte of multi-byte opcodes. For now, the only lead byte
				// supported is CEE_PREFIX1 = 0xFE.
				if (opcode >= 0x100)
					m_pOutputBuffer[offset++] = CEE_PREFIX1;

				// This appears to depend on an implicit conversion from
				// unsigned opcode down to BYTE, to deliberately lose data and have
				// opcode >= 0x100 wrap around to 0.
				m_pOutputBuffer[offset++] = (opcode & 0xFF);
			}

			assert(pInstr->m_opcode < _countof(s_OpCodeFlags));
			auto flags = s_OpCodeFlags[pInstr->m_opcode];
			switch (flags)
			{
			case 0:
				break;
			case 1:
				*reinterpret_cast<INT8 *>(&(pIL[offset])) = pInstr->m_Arg8;
				break;
			case 2:
				*reinterpret_cast<INT16 *>(&(pIL[offset])) = pInstr->m_Arg16;
				break;
			case 4:
				*reinterpret_cast<INT32 *>(&(pIL[offset])) = pInstr->m_Arg32;
				break;
			case 8:
				*reinterpret_cast<INT64 *>(&(pIL[offset])) = pInstr->m_Arg64;
				break;
			case 1 | OPCODEFLAGS_BranchTarget:
				fBranch = true;
				break;
			case 4 | OPCODEFLAGS_BranchTarget:
				fBranch = true;
				break;
			case 0 | OPCODEFLAGS_Switch:
				*reinterpret_cast<INT32 *>(&(pIL[offset])) = pInstr->m_Arg32;
				offset += sizeof(INT32);
				break;
			default:
				assert(false);
				break;
			}
			offset += (flags & OPCODEFLAGS_SizeMask);
		}
		m_IL.m_offset = offset;

		if (fBranch)
		{
			auto fTryAgain = false;
			unsigned switchBase = 0;

			// Go over all control flow instructions and resolve the targets
			for (auto pInstr = m_IL.m_pNext; pInstr != &m_IL; pInstr = pInstr->m_pNext)
			{
				auto opcode = pInstr->m_opcode;

				if (pInstr->m_opcode == CEE_SWITCH)
				{
					switchBase = pInstr->m_offset + 1 + sizeof(INT32) * (pInstr->m_Arg32 + 1);
					continue;
				}
				if (opcode == CEE_SWITCH_ARG)
				{
					// Switch args are special
					*reinterpret_cast<INT32 *>(&(pIL[pInstr->m_offset])) = pInstr->m_pTarget->m_offset - switchBase;
					continue;
				}

				BYTE flags = s_OpCodeFlags[pInstr->m_opcode];

				if (flags & OPCODEFLAGS_BranchTarget)
				{
					int delta = pInstr->m_pTarget->m_offset - pInstr->m_pNext->m_offset;

					switch (flags)
					{
					case 1 | OPCODEFLAGS_BranchTarget:
						// Check if delta is too big to fit into an INT8.
						// 
						// (see #pragma at top of file)
						if (static_cast<INT8>(delta) != delta)
						{
							if (opcode == CEE_LEAVE_S)
							{
								pInstr->m_opcode = CEE_LEAVE;
							}
							else
							{
								assert(opcode >= CEE_BR_S && opcode <= CEE_BLT_UN_S);
								pInstr->m_opcode = opcode - CEE_BR_S + CEE_BR;
								assert(pInstr->m_opcode >= CEE_BR && pInstr->m_opcode <= CEE_BLT_UN);
							}
							fTryAgain = true;
							continue;
						}
						*reinterpret_cast<INT8 *>(&(pIL[pInstr->m_pNext->m_offset - sizeof(INT8)])) = delta;
						break;
					case 4 | OPCODEFLAGS_BranchTarget:
						*reinterpret_cast<INT32 *>(&(pIL[pInstr->m_pNext->m_offset - sizeof(INT32)])) = delta;
						break;
					default:
						assert(false);
						break;
					}
				}
			}

			// Do the whole thing again if we changed the size of some branch targets
			if (fTryAgain)
				goto again;
		}

		auto codeSize = offset;
		unsigned totalSize;
		LPBYTE pBody = nullptr;
		if (m_fGenerateTinyHeader)
		{
			// Make sure we can fit in a tiny header
			if (codeSize >= 64)
				return E_FAIL;

			totalSize = sizeof(IMAGE_COR_ILMETHOD_TINY) + codeSize;
			pBody = AllocateILMemory(totalSize);
			IfNullRet(pBody);

			auto pCurrent = pBody;

			// Here's the tiny header
			*pCurrent = static_cast<BYTE>(CorILMethod_TinyFormat | (codeSize << 2));
			pCurrent += sizeof(IMAGE_COR_ILMETHOD_TINY);

			// And the body
			CopyMemory(pCurrent, m_pOutputBuffer, codeSize);
		}
		else
		{
			// Use FAT header

			auto alignedCodeSize = (offset + 3) & ~3;

			totalSize = sizeof(IMAGE_COR_ILMETHOD_FAT) + alignedCodeSize +
				(m_nEH ? (sizeof(IMAGE_COR_ILMETHOD_SECT_FAT) + sizeof(IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_FAT) * m_nEH) : 0);

			pBody = AllocateILMemory(totalSize);
			IfNullRet(pBody);

			BYTE * pCurrent = pBody;

			auto pHeader = reinterpret_cast<IMAGE_COR_ILMETHOD_FAT *>(pCurrent);
			pHeader->Flags = m_flags | (m_nEH ? CorILMethod_MoreSects : 0) | CorILMethod_FatFormat;
			pHeader->Size = sizeof(IMAGE_COR_ILMETHOD_FAT) / sizeof(DWORD);
			pHeader->MaxStack = m_maxStack;
			pHeader->CodeSize = offset;
			pHeader->LocalVarSigTok = m_tkLocalVarSig;

			pCurrent = reinterpret_cast<BYTE*>(pHeader + 1);

			CopyMemory(pCurrent, m_pOutputBuffer, codeSize);
			pCurrent += alignedCodeSize;

			if (m_nEH != 0)
			{
				auto pEH = reinterpret_cast<IMAGE_COR_ILMETHOD_SECT_FAT *>(pCurrent);
				pEH->Kind = CorILMethod_Sect_EHTable | CorILMethod_Sect_FatFormat;
				pEH->DataSize = static_cast<unsigned>(sizeof(IMAGE_COR_ILMETHOD_SECT_FAT) + sizeof(IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_FAT) * m_nEH);

				pCurrent = reinterpret_cast<BYTE*>(pEH + 1);

				for (unsigned iEH = 0; iEH < m_nEH; iEH++)
				{
					auto pSrc = &(m_pEH[iEH]);
					auto pDst = reinterpret_cast<IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_FAT *>(pCurrent);

					pDst->Flags = pSrc->m_Flags;
					pDst->TryOffset = pSrc->m_pTryBegin->m_offset;
					pDst->TryLength = pSrc->m_pTryEnd->m_offset - pSrc->m_pTryBegin->m_offset;
					pDst->HandlerOffset = pSrc->m_pHandlerBegin->m_offset;
					pDst->HandlerLength = pSrc->m_pHandlerEnd->m_pNext->m_offset - pSrc->m_pHandlerBegin->m_offset;
					if ((pSrc->m_Flags & COR_ILEXCEPTION_CLAUSE_FILTER) == 0)
						pDst->ClassToken = pSrc->m_ClassToken;
					else
						pDst->FilterOffset = pSrc->m_pFilter->m_offset;

					pCurrent = reinterpret_cast<BYTE*>(pDst + 1);
				}
			}
		}

		IfFailRet(SetILFunctionBody(totalSize, pBody));
		DeallocateILMemory(pBody);

		return S_OK;
	}

	HRESULT SetILFunctionBody(unsigned size, LPBYTE pBody)
	{
		if (m_pICorProfilerFunctionControl != nullptr)
		{
			// We're supplying IL for a rejit, so use the rejit mechanism
			IfFailRet(m_pICorProfilerFunctionControl->SetILFunctionBody(size, pBody));
		}
		else
		{
			// "classic-style" instrumentation on first JIT, so use old mechanism
			IfFailRet(m_pICorProfilerInfo->SetILFunctionBody(m_moduleId, m_tkMethod, pBody));
		}

		return S_OK;
	}

	LPBYTE AllocateILMemory(unsigned size)
	{
		if (m_pICorProfilerFunctionControl != nullptr)
		{
			// We're supplying IL for a rejit, so we can just allocate from
			// the heap
			return new BYTE[size];
		}

		// Else, this is "classic-style" instrumentation on first JIT, and
		// need to use the CLR's IL allocator

		if (FAILED(m_pICorProfilerInfo->GetILFunctionBodyAllocator(m_moduleId, &m_pIMethodMalloc)))
			return nullptr;

		return static_cast<LPBYTE>(m_pIMethodMalloc->Alloc(size));
	}

	void DeallocateILMemory(LPBYTE pBody)
	{
		if (m_pICorProfilerFunctionControl == nullptr)
		{
			// Old-style instrumentation does not provide a way to free up bytes
			return;
		}

		delete [] pBody;
	}


	/////////////////////////////////////////////////////////////////////////////////////////////////
	//
	// R E W R I T E
	//
	////////////////////////////////////////////////////////////////////////////////////////////////

	// Probe_XXX are the callbacks to be called from the JITed code

	static void __fastcall Probe_LDSFLD(WCHAR * pFieldName)
	{
		printf("LDSFLD: %S\n", pFieldName);
	}

	static void __fastcall Probe_SDSFLD(WCHAR * pFieldName)
	{
		printf("STSFLD: %S\n", pFieldName);
	}

	UINT AddNewInt32Local()
	{
		HRESULT hr;

		// Here's a buffer into which we will write out the modified signature.  This sample
		// code just bails out if it hits signatures that are too big.  Just one of many reasons
		// why you use this code AT YOUR OWN RISK!
		COR_SIGNATURE rgbNewSig[4096];

		// Use the signature token to look up the actual signature
		PCCOR_SIGNATURE rgbOrigSig = nullptr;
		ULONG cbOrigSig;
		if (m_tkLocalVarSig == mdTokenNil)
		{
			// Function has no locals to begin with
			rgbOrigSig = nullptr;
			cbOrigSig = 0;
		}
		else
		{
			hr = m_pMetaDataImport->GetSigFromToken(m_tkLocalVarSig, &rgbOrigSig, &cbOrigSig);
			if (FAILED(hr))
			{
				return 0;
			}
		}

		// These are our running indices in the original and new signature, respectively
		UINT iOrigSig = 0;
		UINT iNewSig = 0;

		if (cbOrigSig > 0)
		{
			// First byte of signature must identify that it's a locals signature!
			assert(rgbOrigSig[iOrigSig] == SIG_LOCAL_SIG);
			iOrigSig++;
		}

		// Copy SIG_LOCAL_SIG
		if (iNewSig + 1 > sizeof(rgbNewSig))
		{
			// We'll write one byte below but no room!
			return 0;
		}
		rgbNewSig[iNewSig++] = SIG_LOCAL_SIG;

		// Get original count of locals...
		ULONG cOrigLocals;
		if (cbOrigSig == 0)
		{
			// No locals to begin with
			cOrigLocals = 0;
		}
		else
		{
			ULONG cbOrigLocals;
			hr = CorSigUncompressData(&rgbOrigSig[iOrigSig],
				4,                    // [IN] length of the signature
				&cOrigLocals,         // [OUT] the expanded data
				&cbOrigLocals);       // [OUT] length of the expanded data    
			if (FAILED(hr))
			{
				return 0;
			}
			iOrigSig += cbOrigLocals;
		}

		// ...and write new count of locals (cOrigLocals+1)
		if (iNewSig + 4 > sizeof(rgbNewSig))
		{
			// CorSigCompressData will write up to 4 bytes but no room!
			return 0;
		}
		ULONG cbNewLocals;
		cbNewLocals = CorSigCompressData(cOrigLocals+1,         // [IN] given uncompressed data 
			&rgbNewSig[iNewSig]);  // [OUT] buffer where iLen will be compressed and stored.   
		iNewSig += cbNewLocals;

		if (cbOrigSig > 0)
		{
			// Copy the rest
			if (iNewSig + cbOrigSig - iOrigSig > sizeof(rgbNewSig))
			{
				// We'll copy cbOrigSig - iOrigSig bytes, but no room!
				return 0;
			}
			memcpy(&rgbNewSig[iNewSig], &rgbOrigSig[iOrigSig], cbOrigSig-iOrigSig);
			iNewSig += cbOrigSig-iOrigSig;
		}

		// Manually append final local

		if (iNewSig + 1 > sizeof(rgbNewSig))
		{
			// We'll write one byte below but no room!
			return 0;
		}
		rgbNewSig[iNewSig++] = ELEMENT_TYPE_I4;

		// We're done building up the new signature blob.  We now need to add it to
		// the metadata for this module, so we can get a token back for it.
		assert(iNewSig <= sizeof(rgbNewSig));
		hr = m_pMetaDataEmit->GetTokenFromSig(&rgbNewSig[0],      // [IN] Signature to define.    
			iNewSig,            // [IN] Size of signature data. 
			&m_tkLocalVarSig);  // [OUT] returned signature token.  
		if (FAILED(hr))
		{
			return 0;
		}

		// 0-based index of new local = 0-based index of original last local + 1
		//                            = count of original locals
		return cOrigLocals;
	}

	UINT AddNewObjectArrayLocal()
	{
		HRESULT hr;

		// Here's a buffer into which we will write out the modified signature.  This sample
		// code just bails out if it hits signatures that are too big.  Just one of many reasons
		// why you use this code AT YOUR OWN RISK!
		COR_SIGNATURE rgbNewSig[4096];

		// Use the signature token to look up the actual signature
		PCCOR_SIGNATURE rgbOrigSig = nullptr;
		ULONG cbOrigSig;
		if (m_tkLocalVarSig == mdTokenNil)
		{
			// Function has no locals to begin with
			rgbOrigSig = nullptr;
			cbOrigSig = 0;
		}
		else
		{
			hr = m_pMetaDataImport->GetSigFromToken(m_tkLocalVarSig, &rgbOrigSig, &cbOrigSig);
			if (FAILED(hr))
			{
				return 0;
			}
		}

		// These are our running indices in the original and new signature, respectively
		UINT iOrigSig = 0;
		UINT iNewSig = 0;

		if (cbOrigSig > 0)
		{
			// First byte of signature must identify that it's a locals signature!
			assert(rgbOrigSig[iOrigSig] == SIG_LOCAL_SIG);
			iOrigSig++;
		}

		// Copy SIG_LOCAL_SIG
		if (iNewSig + 1 > sizeof(rgbNewSig))
		{
			// We'll write one byte below but no room!
			return 0;
		}
		rgbNewSig[iNewSig++] = SIG_LOCAL_SIG;

		// Get original count of locals...
		ULONG cOrigLocals;
		if (cbOrigSig == 0)
		{
			// No locals to begin with
			cOrigLocals = 0;
		}
		else
		{
			ULONG cbOrigLocals;
			hr = CorSigUncompressData(&rgbOrigSig[iOrigSig],
				4,                    // [IN] length of the signature
				&cOrigLocals,         // [OUT] the expanded data
				&cbOrigLocals);       // [OUT] length of the expanded data    
			if (FAILED(hr))
			{
				return 0;
			}
			iOrigSig += cbOrigLocals;
		}

		// ...and write new count of locals (cOrigLocals+1)
		if (iNewSig + 4 > sizeof(rgbNewSig))
		{
			// CorSigCompressData will write up to 4 bytes but no room!
			return 0;
		}
		ULONG cbNewLocals;
		cbNewLocals = CorSigCompressData(cOrigLocals + 1,         // [IN] given uncompressed data 
			&rgbNewSig[iNewSig]);  // [OUT] buffer where iLen will be compressed and stored.   
		iNewSig += cbNewLocals;

		if (cbOrigSig > 0)
		{
			// Copy the rest
			if (iNewSig + cbOrigSig - iOrigSig > sizeof(rgbNewSig))
			{
				// We'll copy cbOrigSig - iOrigSig bytes, but no room!
				return 0;
			}
			memcpy(&rgbNewSig[iNewSig], &rgbOrigSig[iOrigSig], cbOrigSig - iOrigSig);
			iNewSig += cbOrigSig - iOrigSig;
		}

		// Manually append final local

		if (iNewSig + 5 > sizeof(rgbNewSig))
		{
			// We'll write up to 5 bytes below but no room!
			return 0;
		}

		rgbNewSig[iNewSig++] = ELEMENT_TYPE_ARRAY;
		mdToken tdSystemArray;
		hr = m_pMetaDataImport->FindTypeDefByName(
			L"System.Array",
			mdTokenNil,
			&tdSystemArray);

		unsigned char pDataDout[4] {0};
		//unsigned char tk;
		auto SigTok = CorSigCompressToken(tdSystemArray, &pDataDout);

		rgbNewSig[iNewSig++] = pDataDout[0];

		// We're done building up the new signature blob.  We now need to add it to
		// the metadata for this module, so we can get a token back for it.
		assert(iNewSig <= sizeof(rgbNewSig));
		hr = m_pMetaDataEmit->GetTokenFromSig(&rgbNewSig[0],      // [IN] Signature to define.    
			iNewSig,            // [IN] Size of signature data. 
			&m_tkLocalVarSig);  // [OUT] returned signature token.  
		if (FAILED(hr))
		{
			return 0;
		}

		// 0-based index of new local = 0-based index of original last local + 1
		//                            = count of original locals
		return cOrigLocals;
	}

	UINT AddNewString()
	{
		HRESULT hr;
		auto localstring = L"Test Input String!";
		auto localsize = lstrlenW(localstring);
		mdToken stringToken;
		hr = m_pMetaDataEmit->DefineUserString(localstring, localsize, &stringToken);
		return stringToken;
	}

	WCHAR* GetNameFromToken(mdToken tk)
	{
		mdTypeDef tkClass;

		LPWSTR szField = nullptr;
		ULONG cchField = 0;

again:
		switch (TypeFromToken(tk))
		{
		case mdtFieldDef:
			m_pMetaDataImport->GetFieldProps(tk, &tkClass,
				szField, cchField, &cchField,
				nullptr,
				nullptr, nullptr,
				nullptr, nullptr, nullptr);
			break;

		case mdtMemberRef:
			m_pMetaDataImport->GetMemberRefProps(tk, &tkClass,
				szField, cchField, &cchField,
				nullptr, nullptr);
			break;
		default:
			assert(false);
			break;
		}

		if (szField == nullptr)
		{
			szField = new WCHAR[cchField];
			goto again;
		}

		return szField;
	}

	ILInstr * NewLDC(LPVOID p)
	{
		auto pNewInstr = NewILInstr();
		if (pNewInstr != nullptr)
		{
			if (sizeof(void*) == 4)
			{
				pNewInstr->m_opcode = CEE_LDC_I4;
				pNewInstr->m_Arg32 = static_cast<INT32>(reinterpret_cast<size_t>(p));
			}
			else
			{
				pNewInstr->m_opcode = CEE_LDC_I8;
				pNewInstr->m_Arg64 = static_cast<INT64>(reinterpret_cast<size_t>(p));
			}
		}
		return pNewInstr;
	}
};


HRESULT AddProbe(
	ILRewriter * pilr,
	ModuleID moduleID,
	mdMethodDef methodDef, 
	int nVersion,
	UINT iLocalVersion,
	mdToken mdProbeRef,
	ILInstr * pInsertProbeBeforeThisInstr)
{
	// Add a call before the instruction stream:
	// 
	// Replace
	// 
	//     ...
	//     pInsertProbeBeforeThisInstr
	//     ...
	// 
	// with
	// 
	//     ...
	// #ifdef _X86_
	//     ldc.i4 moduleID
	// #else
	//     ldc.i8 moduleID
	// #endif
	//     ldc.i4 mdMethodDefCur
	//     ldc.i4 VersionNumberCur
	//     stloc LocalVarUsedForVersion
	//     ldloc LocalVarUsedForVersion
	//     call MgdEnteredFunction32/64
	//     pInsertProbeBeforeThisInstr
	//     ...

	//     ldc.i4/8 moduleID
	auto pNewInstr = pilr->NewILInstr();
#ifdef _WIN64
	pNewInstr->m_opcode = CEE_LDC_I8;
	pNewInstr->m_Arg64 = moduleID;
#else
	pNewInstr->m_opcode = CEE_LDC_I4;
	pNewInstr->m_Arg32 = moduleID;
#endif
	pilr->InsertBefore(pInsertProbeBeforeThisInstr, pNewInstr);

	//     ldc.i4 mdMethodDefCur
	pNewInstr = pilr->NewILInstr();
	pNewInstr->m_opcode = CEE_LDC_I4;
	pNewInstr->m_Arg32 = methodDef;
	pilr->InsertBefore(pInsertProbeBeforeThisInstr, pNewInstr);

	//     ldc.i4 VersionNumberCur
	pNewInstr = pilr->NewILInstr();
	pNewInstr->m_opcode = CEE_LDC_I4;
	pNewInstr->m_Arg32 = nVersion;
	pilr->InsertBefore(pInsertProbeBeforeThisInstr, pNewInstr);

	//     stloc LocalVarUsedForVersion
	pNewInstr = pilr->NewILInstr();
	pNewInstr->m_opcode = CEE_STLOC;
	pNewInstr->m_Arg16 = static_cast<INT16>(iLocalVersion);
	pilr->InsertBefore(pInsertProbeBeforeThisInstr, pNewInstr);

	//     ldloc LocalVarUsedForVersion
	pNewInstr = pilr->NewILInstr();
	pNewInstr->m_opcode = CEE_LDLOC;
	pNewInstr->m_Arg16 = static_cast<INT16>(iLocalVersion);
	pilr->InsertBefore(pInsertProbeBeforeThisInstr, pNewInstr);


	//     call MgdEnteredFunction32/64 (may be via memberRef or methodDef)
	pNewInstr = pilr->NewILInstr();
	pNewInstr->m_opcode = CEE_CALL;
	pNewInstr->m_Arg32 = mdProbeRef;
	pilr->InsertBefore(pInsertProbeBeforeThisInstr, pNewInstr);

	return S_OK;
}

HRESULT AddProbeSA(
	ILRewriter * pilr,
	ModuleID moduleID,
	mdMethodDef methodDef,
	int nVersion,
	UINT iLocalVersion,
	mdToken mdProbeRef,
	mdToken mdSARef,
	ILInstr * pInsertProbeBeforeThisInstr)
{
	// Add a call before the instruction stream:
	// 
	// Replace
	// 
	//     ...
	//     pInsertProbeBeforeThisInstr
	//     ...
	// 
	// with
	// 
	//     ...
	// #ifdef _X86_
	//     ldc.i4 moduleID
	// #else
	//     ldc.i8 moduleID
	// #endif
	//     ldc.i4 mdMethodDefCur
	//     ldc.i4 VersionNumberCur
	//     stloc LocalVarUsedForVersion
	//     ldloc LocalVarUsedForVersion
	//     call MgdEnteredFunction32/64
	//     pInsertProbeBeforeThisInstr
	//     ...

	//     ldc.i4/8 moduleID
	auto pNewInstr = pilr->NewILInstr();
#ifdef _WIN64
	pNewInstr->m_opcode = CEE_LDC_I8;
	pNewInstr->m_Arg64 = moduleID;
#else
	pNewInstr->m_opcode = CEE_LDC_I4;
	pNewInstr->m_Arg32 = moduleID;
#endif
	pilr->InsertBefore(pInsertProbeBeforeThisInstr, pNewInstr);

	//     ldc.i4 mdMethodDefCur
	pNewInstr = pilr->NewILInstr();
	pNewInstr->m_opcode = CEE_LDC_I4;
	pNewInstr->m_Arg32 = methodDef;
	pilr->InsertBefore(pInsertProbeBeforeThisInstr, pNewInstr);

	//     ldc.i4 VersionNumberCur
	pNewInstr = pilr->NewILInstr();
	pNewInstr->m_opcode = CEE_LDC_I4;
	pNewInstr->m_Arg32 = 5;
	pilr->InsertBefore(pInsertProbeBeforeThisInstr, pNewInstr);

	//     stloc LocalVarUsedForVersion
	pNewInstr = pilr->NewILInstr();
	pNewInstr->m_opcode = CEE_NEWARR;
	pNewInstr->m_Arg16 = static_cast<INT16>(iLocalVersion);
	pilr->InsertBefore(pInsertProbeBeforeThisInstr, pNewInstr);

	//     ldloc LocalVarUsedForVersion
	pNewInstr = pilr->NewILInstr();
	pNewInstr->m_opcode = CEE_LDLOC;
	pNewInstr->m_Arg16 = static_cast<INT16>(iLocalVersion);
	pilr->InsertBefore(pInsertProbeBeforeThisInstr, pNewInstr);


	//     call MgdEnteredFunction32/64 (may be via memberRef or methodDef)
	pNewInstr = pilr->NewILInstr();
	pNewInstr->m_opcode = CEE_CALL;
	pNewInstr->m_Arg32 = mdProbeRef;
	pilr->InsertBefore(pInsertProbeBeforeThisInstr, pNewInstr);

	return S_OK;
}



HRESULT AddProbe2(
	ILRewriter * pilr,
	ModuleID moduleID,
	mdMethodDef methodDef,
	mdToken sString,
	mdToken mdProbeRef,
	ILInstr * pInsertProbeBeforeThisInstr)
{
	// Add a call before the instruction stream:
	// 
	// Replace
	// 
	//     ...
	//     pInsertProbeBeforeThisInstr
	//     ...
	// 
	// with
	// 
	//     ...
	// #ifdef _X86_
	//     ldc.i4 moduleID
	// #else
	//     ldc.i8 moduleID
	// #endif
	//     ldc.i4 mdMethodDefCur
	//     ldc.i4 VersionNumberCur
	//     stloc LocalVarUsedForVersion
	//     ldloc LocalVarUsedForVersion
	//     call MgdEnteredFunction32/64
	//     pInsertProbeBeforeThisInstr
	//     ...

	//     ldc.i4/8 moduleID
	auto pNewInstr = pilr->NewILInstr();
#ifdef _WIN64
	pNewInstr->m_opcode = CEE_LDC_I8;
	pNewInstr->m_Arg64 = moduleID;
#else
	pNewInstr->m_opcode = CEE_LDC_I4;
	pNewInstr->m_Arg32 = moduleID;
#endif
	pilr->InsertBefore(pInsertProbeBeforeThisInstr, pNewInstr);

	//     ldc.i4 mdMethodDefCur
	pNewInstr = pilr->NewILInstr();
	pNewInstr->m_opcode = CEE_LDC_I4;
	pNewInstr->m_Arg32 = methodDef;
	pilr->InsertBefore(pInsertProbeBeforeThisInstr, pNewInstr);

	
	//     ldc.i4 mdMethodDefCur
	pNewInstr = pilr->NewILInstr();
	pNewInstr->m_opcode = CEE_LDSTR;
	pNewInstr->m_Arg32 = sString;
	pilr->InsertBefore(pInsertProbeBeforeThisInstr, pNewInstr);

	//     call MgdEnteredFunction32/64 (may be via memberRef or methodDef)
	pNewInstr = pilr->NewILInstr();
	pNewInstr->m_opcode = CEE_CALL;
	pNewInstr->m_Arg32 = mdProbeRef;
	pilr->InsertBefore(pInsertProbeBeforeThisInstr, pNewInstr);

	return S_OK;
}

HRESULT AddProbeAddFunctions(
	ILRewriter * pilr,
	ModuleID moduleID,
	mdMethodDef methodDef,
	mdToken sString,
	mdToken mdProbeRef,
	ILInstr * pInsertProbeBeforeThisInstr)
{
	// Add a call before the instruction stream:
	// 
	// Replace
	// 
	//     ...
	//     pInsertProbeBeforeThisInstr
	//     ...
	// 
	// with
	// 
	//     ...
	// #ifdef _X86_
	//     ldc.i4 moduleID
	// #else
	//     ldc.i8 moduleID
	// #endif
	//     ldc.i4 mdMethodDefCur
	//     ldc.i4 VersionNumberCur
	//     ldarg_0
	//     ldarg_1
	//     call MgdEnteredFunction32/64
	//     pInsertProbeBeforeThisInstr
	//     ...

	//     ldc.i4/8 moduleID
	auto pNewInstr = pilr->NewILInstr();
#ifdef _WIN64
	pNewInstr->m_opcode = CEE_LDC_I8;
	pNewInstr->m_Arg64 = moduleID;
#else
	pNewInstr->m_opcode = CEE_LDC_I4;
	pNewInstr->m_Arg32 = moduleID;
#endif
	pilr->InsertBefore(pInsertProbeBeforeThisInstr, pNewInstr);

	//     ldc.i4 mdMethodDefCur
	pNewInstr = pilr->NewILInstr();
	pNewInstr->m_opcode = CEE_LDC_I4;
	pNewInstr->m_Arg32 = methodDef;
	pilr->InsertBefore(pInsertProbeBeforeThisInstr, pNewInstr);


	//     ldc.i4 mdMethodDefCur
	pNewInstr = pilr->NewILInstr();
	pNewInstr->m_opcode = CEE_LDARG_0;
	pilr->InsertBefore(pInsertProbeBeforeThisInstr, pNewInstr);

	//     ldc.i4 mdMethodDefCur
	pNewInstr = pilr->NewILInstr();
	pNewInstr->m_opcode = CEE_LDARG_1;
	pilr->InsertBefore(pInsertProbeBeforeThisInstr, pNewInstr);

	//     call MgdEnteredFunction32/64 (may be via memberRef or methodDef)
	pNewInstr = pilr->NewILInstr();
	pNewInstr->m_opcode = CEE_CALL;
	pNewInstr->m_Arg32 = mdProbeRef;
	pilr->InsertBefore(pInsertProbeBeforeThisInstr, pNewInstr);

	return S_OK;
}


HRESULT AddEnterProbe(
	ILRewriter * pilr,
	ModuleID moduleID,
	mdMethodDef methodDef, 
	int nVersion,
	UINT iLocalVersion, 
	mdToken mdEnterProbeRef)
{
	auto pFirstOriginalInstr = pilr->GetILList()->m_pNext;

	return AddProbe(pilr, moduleID, methodDef, nVersion, iLocalVersion, mdEnterProbeRef, pFirstOriginalInstr);
}


HRESULT AddExitProbe(
	ILRewriter * pilr,
	ModuleID moduleID,
	mdMethodDef methodDef, 
	int nVersion,
	UINT iLocalVersion, 
	mdToken mdExitProbeRef)
{
	HRESULT hr;
	auto fAtLeastOneProbeAdded = FALSE;

	// Find all RETs, and insert a call to the exit probe before each one.
	for (ILInstr * pInstr = pilr->GetILList()->m_pNext; pInstr != pilr->GetILList(); pInstr = pInstr->m_pNext)
	{
		switch (pInstr->m_opcode)
		{
		case CEE_RET:
			{
				// We want any branches or leaves that targeted the RET instruction to
				// actually target the epilog instructions we're adding. So turn the "RET"
				// into ["NOP", "RET"], and THEN add the epilog between the NOP & RET. That
				// ensures that any branches that went to the RET will now go to the NOP and
				// then execute our epilog.

				// RET->NOP
				pInstr->m_opcode = CEE_NOP;

				// Add the new RET after
				auto pNewRet = pilr->NewILInstr();
				pNewRet->m_opcode = CEE_RET;
				pilr->InsertAfter(pInstr, pNewRet);

				// Add now insert the epilog before the new RET
				hr = AddProbe(pilr, moduleID, methodDef, nVersion, iLocalVersion, mdExitProbeRef, pNewRet);
				if (FAILED(hr))
					return hr;
				fAtLeastOneProbeAdded = TRUE;

				// Advance pInstr after all this gunk so the for loop continues properly
				pInstr = pNewRet;
				break;
			}

		default:
			break;
		}
	}

	if (!fAtLeastOneProbeAdded)
		return E_FAIL;

	return S_OK;
}

HRESULT AddEnterProbe2(
	ILRewriter * pilr,
	ModuleID moduleID,
	mdMethodDef methodDef,
	mdToken sString,
	mdToken mdEnterProbeRef)
{
	auto pFirstOriginalInstr = pilr->GetILList()->m_pNext;

	return AddProbeAddFunctions(pilr, moduleID, methodDef, sString, mdEnterProbeRef, pFirstOriginalInstr);
}


HRESULT AddExitProbe2(
	ILRewriter * pilr,
	ModuleID moduleID,
	mdMethodDef methodDef,
	mdToken sString,
	mdToken mdExitProbeRef)
{
	HRESULT hr;
	auto fAtLeastOneProbeAdded = FALSE;

	// Find all RETs, and insert a call to the exit probe before each one.
	for (auto pInstr = pilr->GetILList()->m_pNext; pInstr != pilr->GetILList(); pInstr = pInstr->m_pNext)
	{
		switch (pInstr->m_opcode)
		{
		case CEE_RET:
		{
			// We want any branches or leaves that targeted the RET instruction to
			// actually target the epilog instructions we're adding. So turn the "RET"
			// into ["NOP", "RET"], and THEN add the epilog between the NOP & RET. That
			// ensures that any branches that went to the RET will now go to the NOP and
			// then execute our epilog.

			// RET->NOP
			pInstr->m_opcode = CEE_NOP;

			// Add the new RET after
			auto pNewRet = pilr->NewILInstr();
			pNewRet->m_opcode = CEE_RET;
			pilr->InsertAfter(pInstr, pNewRet);

			// Add now insert the epilog before the new RET
			hr = AddProbe2(pilr, moduleID, methodDef, sString, mdExitProbeRef, pNewRet);
			if (FAILED(hr))
				return hr;
			fAtLeastOneProbeAdded = TRUE;

			// Advance pInstr after all this gunk so the for loop continues properly
			pInstr = pNewRet;
			break;
		}

		default:
			break;
		}
	}

	if (!fAtLeastOneProbeAdded)
		return E_FAIL;

	return S_OK;
}



// Uses the general-purpose ILRewriter class to import original
// IL, rewrite it, and send the result to the CLR
HRESULT RewriteIL(
	ICorProfilerInfo * pICorProfilerInfo,
	ICorProfilerFunctionControl * pICorProfilerFunctionControl,
	ModuleID moduleID,
	mdMethodDef methodDef,
	int nVersion,
	mdToken mdEnterProbeRef,
	mdToken mdExitProbeRef)
{
	ILRewriter rewriter(pICorProfilerInfo, pICorProfilerFunctionControl, moduleID, methodDef);

	IfFailRet(rewriter.Initialize());
	IfFailRet(rewriter.Import());
	if (nVersion != 0)
	{
		// Adds enter/exit probes
		assert(mdEnterProbeRef != mdTokenNil);
		assert(mdExitProbeRef != mdTokenNil);
		auto iLocalVersion = rewriter.AddNewInt32Local();
		auto stringToken = rewriter.AddNewString();
		IfFailRet(AddEnterProbe(&rewriter, moduleID, methodDef, nVersion, iLocalVersion, mdEnterProbeRef));
		IfFailRet(AddExitProbe(&rewriter, moduleID, methodDef, nVersion, iLocalVersion, mdExitProbeRef));
	}
	IfFailRet(rewriter.Export());

	return S_OK;
}

// Uses the general-purpose ILRewriter class to import original
// IL, rewrite it, and send the result to the CLR
HRESULT RewriteIL2(
	ICorProfilerInfo * pICorProfilerInfo,
	ICorProfilerFunctionControl * pICorProfilerFunctionControl,
	ModuleID moduleID,
	mdMethodDef methodDef,
	int nVersion,
	mdToken mdEnterProbeRef,
	mdToken mdExitProbeRef)
{
	ILRewriter rewriter(pICorProfilerInfo, pICorProfilerFunctionControl, moduleID, methodDef);

	IfFailRet(rewriter.Initialize());
	IfFailRet(rewriter.Import());
	if (nVersion != 0)
	{
		// Adds enter/exit probes
		assert(mdEnterProbeRef != mdTokenNil);
		assert(mdExitProbeRef != mdTokenNil);
		auto stringToken = rewriter.AddNewString();
		IfFailRet(AddEnterProbe2(&rewriter, moduleID, methodDef, stringToken, mdEnterProbeRef));
		IfFailRet(AddExitProbe2(&rewriter, moduleID, methodDef, stringToken, mdExitProbeRef));
	}
	IfFailRet(rewriter.Export());

	return S_OK;
}


// Uses the general-purpose ILRewriter class to create IL for
// helper probes from scratch.  This is used when the profiler is
// run in the mode to pump helper methods directly into mscorlib,
// rather than using static definitions of them in ProfilerHelper.dll
HRESULT SetILForManagedHelper(
	ICorProfilerInfo * pICorProfilerInfo,
	ModuleID moduleID,
	mdMethodDef mdHelperToAdd,
	mdMethodDef mdIntPtrExplicitCast,
	mdMethodDef mdPInvokeToCall)
{
	ILRewriter rewriter(
		pICorProfilerInfo,
		nullptr, // no ICorProfilerFunctionControl for classic-style on-first-JIT instrumentation
		moduleID, 
		mdHelperToAdd);

	rewriter.InitializeTiny();

	auto pFirstOriginalInstr = rewriter.GetILList()->m_pNext;

	// nop
	auto pNewInstr = rewriter.NewILInstr();
	pNewInstr->m_opcode = CEE_NOP;
	rewriter.InsertBefore(pFirstOriginalInstr, pNewInstr);

	// ldarg.0 (uint32/uint64 ModuleIDCur)
	pNewInstr = rewriter.NewILInstr();
	pNewInstr->m_opcode = CEE_LDARG_0;
	rewriter.InsertBefore(pFirstOriginalInstr, pNewInstr);

	// conv.u8 (cast ModuleIDCur to a managed U8)
	pNewInstr = rewriter.NewILInstr();
	pNewInstr->m_opcode = CEE_CONV_U8;
	rewriter.InsertBefore(pFirstOriginalInstr, pNewInstr);

	// call System.IntPtr::op_Explicit(int64) (convert ModuleIDCur to native int)
	pNewInstr = rewriter.NewILInstr();
	pNewInstr->m_opcode = CEE_CALL;
	pNewInstr->m_Arg32 = mdIntPtrExplicitCast;
	rewriter.InsertBefore(pFirstOriginalInstr, pNewInstr);

	// ldarg.1 (uint32 methodDef of function being entered/exited)
	pNewInstr = rewriter.NewILInstr();
	pNewInstr->m_opcode = CEE_LDARG_1;
	rewriter.InsertBefore(pFirstOriginalInstr, pNewInstr);

	// ldarg.2 (int32 rejitted version number of function being entered/exited)
	pNewInstr = rewriter.NewILInstr();
	pNewInstr->m_opcode = CEE_LDARG_2;
	rewriter.InsertBefore(pFirstOriginalInstr, pNewInstr);

	// call the PInvoke, which will target the profiler's NtvEnter/ExitFunction
	pNewInstr = rewriter.NewILInstr();
	pNewInstr->m_opcode = CEE_CALL;
	pNewInstr->m_Arg32 = mdPInvokeToCall;
	rewriter.InsertBefore(pFirstOriginalInstr, pNewInstr);

	// nop
	pNewInstr = rewriter.NewILInstr();
	pNewInstr->m_opcode = CEE_NOP;
	rewriter.InsertBefore(pFirstOriginalInstr, pNewInstr);

	// ret
	pNewInstr = rewriter.NewILInstr();
	pNewInstr->m_opcode = CEE_RET;
	rewriter.InsertBefore(pFirstOriginalInstr, pNewInstr);

	IfFailRet(rewriter.Export());

	return S_OK;
}

// Uses the general-purpose ILRewriter class to create IL for
// helper probes from scratch.  This is used when the profiler is
// run in the mode to pump helper methods directly into mscorlib,
// rather than using static definitions of them in ProfilerHelper.dll
HRESULT SetILForManagedHelper2(
	ICorProfilerInfo * pICorProfilerInfo,
	ModuleID moduleID,
	mdMethodDef mdHelperToAdd,
	mdMethodDef mdIntPtrExplicitCast,
	mdMethodDef mdPInvokeToCall)
{
	ILRewriter rewriter(
		pICorProfilerInfo,
		nullptr, // no ICorProfilerFunctionControl for classic-style on-first-JIT instrumentation
		moduleID,
		mdHelperToAdd);

	rewriter.InitializeTiny();

	auto pFirstOriginalInstr = rewriter.GetILList()->m_pNext;

	// nop
	auto pNewInstr = rewriter.NewILInstr();
	pNewInstr->m_opcode = CEE_NOP;
	rewriter.InsertBefore(pFirstOriginalInstr, pNewInstr);

	// ldarg.0 (uint32/uint64 ModuleIDCur)
	pNewInstr = rewriter.NewILInstr();
	pNewInstr->m_opcode = CEE_LDARG_0;
	rewriter.InsertBefore(pFirstOriginalInstr, pNewInstr);

	// conv.u8 (cast ModuleIDCur to a managed U8)
	pNewInstr = rewriter.NewILInstr();
	pNewInstr->m_opcode = CEE_CONV_U8;
	rewriter.InsertBefore(pFirstOriginalInstr, pNewInstr);

	// call System.IntPtr::op_Explicit(int64) (convert ModuleIDCur to native int)
	pNewInstr = rewriter.NewILInstr();
	pNewInstr->m_opcode = CEE_CALL;
	pNewInstr->m_Arg32 = mdIntPtrExplicitCast;
	rewriter.InsertBefore(pFirstOriginalInstr, pNewInstr);

	// ldarg.1 (uint32 methodDef of function being entered/exited)
	pNewInstr = rewriter.NewILInstr();
	pNewInstr->m_opcode = CEE_LDARG_1;
	rewriter.InsertBefore(pFirstOriginalInstr, pNewInstr);

	// ldarg.2 (int32 rejitted version number of function being entered/exited)
	pNewInstr = rewriter.NewILInstr();
	pNewInstr->m_opcode = CEE_LDARG_2;
	rewriter.InsertBefore(pFirstOriginalInstr, pNewInstr);

	// call the PInvoke, which will target the profiler's NtvEnter/ExitFunction
	pNewInstr = rewriter.NewILInstr();
	pNewInstr->m_opcode = CEE_CALL;
	pNewInstr->m_Arg32 = mdPInvokeToCall;
	rewriter.InsertBefore(pFirstOriginalInstr, pNewInstr);

	// nop
	pNewInstr = rewriter.NewILInstr();
	pNewInstr->m_opcode = CEE_NOP;
	rewriter.InsertBefore(pFirstOriginalInstr, pNewInstr);

	// ret
	pNewInstr = rewriter.NewILInstr();
	pNewInstr->m_opcode = CEE_RET;
	rewriter.InsertBefore(pFirstOriginalInstr, pNewInstr);

	IfFailRet(rewriter.Export());

	return S_OK;
}

HRESULT SetILForManagedHelperAddNumbers(
	ICorProfilerInfo * pICorProfilerInfo,
	ModuleID moduleID,
	mdMethodDef mdHelperToAdd,
	mdMethodDef mdIntPtrExplicitCast,
	mdMethodDef mdPInvokeToCall)
{
	ILRewriter rewriter(
		pICorProfilerInfo,
		nullptr, // no ICorProfilerFunctionControl for classic-style on-first-JIT instrumentation
		moduleID,
		mdHelperToAdd);

	rewriter.InitializeTiny();

	auto pFirstOriginalInstr = rewriter.GetILList()->m_pNext;

	// nop
	auto pNewInstr = rewriter.NewILInstr();
	pNewInstr->m_opcode = CEE_NOP;
	rewriter.InsertBefore(pFirstOriginalInstr, pNewInstr);

	// ldarg.0 (uint32/uint64 ModuleIDCur)
	pNewInstr = rewriter.NewILInstr();
	pNewInstr->m_opcode = CEE_LDARG_0;
	rewriter.InsertBefore(pFirstOriginalInstr, pNewInstr);

	// conv.u8 (cast ModuleIDCur to a managed U8)
	pNewInstr = rewriter.NewILInstr();
	pNewInstr->m_opcode = CEE_CONV_U8;
	rewriter.InsertBefore(pFirstOriginalInstr, pNewInstr);

	// call System.IntPtr::op_Explicit(int64) (convert ModuleIDCur to native int)
	pNewInstr = rewriter.NewILInstr();
	pNewInstr->m_opcode = CEE_CALL;
	pNewInstr->m_Arg32 = mdIntPtrExplicitCast;
	rewriter.InsertBefore(pFirstOriginalInstr, pNewInstr);

	// ldarg.1 (uint32 methodDef of function being entered/exited)
	pNewInstr = rewriter.NewILInstr();
	pNewInstr->m_opcode = CEE_LDARG_1;
	rewriter.InsertBefore(pFirstOriginalInstr, pNewInstr);

	// ldarg.2 (int32 rejitted version number of function being entered/exited)
	pNewInstr = rewriter.NewILInstr();
	pNewInstr->m_opcode = CEE_LDARG_2;
	rewriter.InsertBefore(pFirstOriginalInstr, pNewInstr);

	// ldarg.1 (uint32 methodDef of function being entered/exited)
	pNewInstr = rewriter.NewILInstr();
	pNewInstr->m_opcode = CEE_LDARG_3;
	rewriter.InsertBefore(pFirstOriginalInstr, pNewInstr);

	// call the PInvoke, which will target the profiler's NtvEnter/ExitFunction
	pNewInstr = rewriter.NewILInstr();
	pNewInstr->m_opcode = CEE_CALL;
	pNewInstr->m_Arg32 = mdPInvokeToCall;
	rewriter.InsertBefore(pFirstOriginalInstr, pNewInstr);

	// nop
	pNewInstr = rewriter.NewILInstr();
	pNewInstr->m_opcode = CEE_NOP;
	rewriter.InsertBefore(pFirstOriginalInstr, pNewInstr);

	// ret
	pNewInstr = rewriter.NewILInstr();
	pNewInstr->m_opcode = CEE_RET;
	rewriter.InsertBefore(pFirstOriginalInstr, pNewInstr);

	IfFailRet(rewriter.Export());

	return S_OK;
}


HRESULT SetILForManagedHelperSA(
	ICorProfilerInfo * pICorProfilerInfo,
	ModuleID moduleID,
	mdMethodDef mdHelperToAdd,
	mdMethodDef mdIntPtrExplicitCast,
	mdMethodDef mdPInvokeToCall,
	mdTypeRef mdObject)
{
	ILRewriter rewriter(
		pICorProfilerInfo,
		nullptr, // no ICorProfilerFunctionControl for classic-style on-first-JIT instrumentation
		moduleID,
		mdHelperToAdd);

	rewriter.InitializeTiny();

	auto pFirstOriginalInstr = rewriter.GetILList()->m_pNext;
	
	// nop
	auto pNewInstr = rewriter.NewILInstr();
	pNewInstr->m_opcode = CEE_NOP;
	rewriter.InsertBefore(pFirstOriginalInstr, pNewInstr);

	// ldarg.0 (uint32/uint64 ModuleIDCur)
	pNewInstr = rewriter.NewILInstr();
	pNewInstr->m_opcode = CEE_LDARG_0;
	rewriter.InsertBefore(pFirstOriginalInstr, pNewInstr);

	// conv.u8 (cast ModuleIDCur to a managed U8)
	pNewInstr = rewriter.NewILInstr();
	pNewInstr->m_opcode = CEE_CONV_U8;
	rewriter.InsertBefore(pFirstOriginalInstr, pNewInstr);

	// call System.IntPtr::op_Explicit(int64) (convert ModuleIDCur to native int)
	pNewInstr = rewriter.NewILInstr();
	pNewInstr->m_opcode = CEE_CALL;
	pNewInstr->m_Arg32 = mdIntPtrExplicitCast;
	rewriter.InsertBefore(pFirstOriginalInstr, pNewInstr);

	// ldarg.1 (uint32 methodDef of function being entered/exited)
	pNewInstr = rewriter.NewILInstr();
	pNewInstr->m_opcode = CEE_LDARG_1;
	rewriter.InsertBefore(pFirstOriginalInstr, pNewInstr);

	// ldarg.1 (uint32 methodDef of function being entered / exited)
	pNewInstr = rewriter.NewILInstr();
	pNewInstr->m_opcode = CEE_LDC_I4;
	pNewInstr->m_Arg32 = 1;
	rewriter.InsertBefore(pFirstOriginalInstr, pNewInstr);

	// ldarg.2 (int32 rejitted version number of function being entered/exited)
	pNewInstr = rewriter.NewILInstr();
	pNewInstr->m_opcode = CEE_NEWARR;
	pNewInstr->m_Arg32 = mdObject;
	rewriter.InsertBefore(pFirstOriginalInstr, pNewInstr);

	// ldarg.1 (uint32 methodDef of function being entered / exited)
	pNewInstr = rewriter.NewILInstr();
	pNewInstr->m_opcode = CEE_LDC_I4;
	pNewInstr->m_Arg32 = 1;
	rewriter.InsertBefore(pFirstOriginalInstr, pNewInstr);

	// ldarg.1 (uint32 methodDef of function being entered / exited)
	pNewInstr = rewriter.NewILInstr();
	pNewInstr->m_opcode = CEE_LDC_I4;
	pNewInstr->m_Arg32 = 0xFFFF;
	rewriter.InsertBefore(pFirstOriginalInstr, pNewInstr);

	// ldarg.1 (uint32 methodDef of function being entered/exited)
	pNewInstr = rewriter.NewILInstr();
	pNewInstr->m_opcode = CEE_STELEM_REF;
	rewriter.InsertBefore(pFirstOriginalInstr, pNewInstr);

	// call the PInvoke, which will target the profiler's NtvEnter/ExitFunction
	pNewInstr = rewriter.NewILInstr();
	pNewInstr->m_opcode = CEE_CALL;
	pNewInstr->m_Arg32 = mdPInvokeToCall;
	rewriter.InsertBefore(pFirstOriginalInstr, pNewInstr);

	// nop
	pNewInstr = rewriter.NewILInstr();
	pNewInstr->m_opcode = CEE_NOP;
	rewriter.InsertBefore(pFirstOriginalInstr, pNewInstr);

	// ret
	pNewInstr = rewriter.NewILInstr();
	pNewInstr->m_opcode = CEE_RET;
	rewriter.InsertBefore(pFirstOriginalInstr, pNewInstr);

	IfFailRet(rewriter.Export());

	return S_OK;
}