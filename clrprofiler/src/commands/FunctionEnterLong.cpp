#include "stdafx.h"
#include "FunctionEnterLong.h"

namespace Commands{

	FunctionEnterLong::FunctionEnterLong(InformationClasses::FunctionInfo funcinfo) 
		: m_data(funcinfo), code(0x0)
	{
	}

	FunctionEnterLong::FunctionEnterLong(FunctionEnterLong&& funcinfo) 
	{
		m_data = funcinfo.m_data;
		m_internalvector = (funcinfo.m_internalvector);
	}

	FunctionEnterLong::~FunctionEnterLong()
	{
	}

	std::wstring FunctionEnterLong::Name()
	{
		return L"Function Enter (Long)";
	}

	std::wstring FunctionEnterLong::Description()
	{
		return L"Sends over an enter method, from the ELT calls, this method will include threads, function ids, parameters, and return values";
	}

	std::shared_ptr<std::vector<char>> FunctionEnterLong::Encode()
	{
		if (!hasEncoded)
		{
#pragma warning(suppress : 4267) // I'm only sending max 4k of data in one command however, the length() prop is __int64. This is valid.
			__int32 size = 4 + 1 + sizeof(FunctionID) + 2;

			m_internalvector = std::vector<char>(size);

			auto intchar = reinterpret_cast<char*>(&size);
			auto func = reinterpret_cast<char*>(&m_data);

			for (size_t i = 0; i < 4; i++)
			{
				m_internalvector[i] = intchar[i];
			}

			m_internalvector[4] = 0x02;

			for (size_t i = 0; i < sizeof(FunctionID); i++)
			{
				m_internalvector[i + 5] = func[i];
			}
			for (size_t i = 0; i < 2; i++)
			{
				m_internalvector[i + size - 2] = 0x00;
			}
			hasEncoded = true;
		}

		return std::make_shared<std::vector<char> >(m_internalvector);
	}

	std::shared_ptr<ICommand> FunctionEnterLong::Decode(std::shared_ptr<std::vector<char>> &data)
	{
		return std::make_shared<FunctionEnterLong>(FunctionEnterLong(InformationClasses::FunctionInfo()));
	}
}