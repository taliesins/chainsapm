using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Collections.ObjectModel;
using System.Windows.Data;
using System.ComponentModel.DataAnnotations;

namespace CommandCreator
{
    public class NetCmdMultiValue : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return values;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    [DataContract]
    public class NetworkCommandViewModel : INotifyPropertyChanged
    {
        public string SaveDirectory { get; set; }
        public TypeHelper TypeHelp { get; internal set; }
        private bool _hasChanges;

        public bool HasChanges
        {
            get { return _hasChanges; }
            set
            {
                _hasChanges = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("SelectedCommand"));
                }
            }
        }

        private NetworkCommand _netCommand;
        public NetworkCommand SelectedCommand
        {
            get { return _netCommand; }
            set
            {
                _netCommand = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("SelectedCommand"));
                }
            }
        }

        private string _selectedType;
        public string SelectedType
        {
            get
            {
                if (string.IsNullOrEmpty(_selectedType))
                {
                    return typeof(Int32).FullName;
                }
                return _selectedType;
            }
            set
            {
                _selectedType = value;
                
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("SelectedType"));
                }
            }
        }

        private ObservableCollection<TypeMap> _csTypeMapping;
        [DataMember(Order = 1)]
        public ObservableCollection<TypeMap> CSharpTypeMapping
        {
            get { return _csTypeMapping; }
            set
            {
                _csTypeMapping = value;
                if (_csTypeMapping != null)
                {
                    _csTypeMapping.CollectionChanged += CollectionChanged;
                }
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("CSharpTypeMapping"));
                }
            }
        }

        void CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            HasChanges = true;
        }

        private ObservableCollection<TypeMap> _cppTypeMapping;
        [DataMember(Order = 2)]
        public ObservableCollection<TypeMap> CppTypeMapping
        {
            get { return _cppTypeMapping; }
            set
            {
                _cppTypeMapping = value;
                if (_cppTypeMapping != null)
                {
                    _cppTypeMapping.CollectionChanged += CollectionChanged;
                }
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("CppTypeMapping"));
                }

            }
        }

        private ObservableCollection<NetworkCommandTreeItemModel> _commands;
        [DataMember(Order = 3)]
        public ObservableCollection<NetworkCommandTreeItemModel> NetworkCommands
        {
            get { return _commands; }
            set
            {
                _commands = value;
                if (_commands != null)
                {
                    _commands.CollectionChanged += CollectionChanged;
                }
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("NetworkCommands"));
                }
            }
        }

        public NetworkCommandViewModel()
        {
            TypeHelp = new TypeHelper();
            CppTypeMapping = new ObservableCollection<TypeMap>();
            CSharpTypeMapping = new ObservableCollection<TypeMap>();
            NetworkCommands = new ObservableCollection<NetworkCommandTreeItemModel>();
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class NetworkCommandTreeItemModel : INotifyPropertyChanged
    {
        private bool _commandNameRegister;

        private NetworkCommand _command;
        public NetworkCommand NetworkCommand
        {
            get { return _command; }
            set
            {
                _command = value;
                if (!_commandNameRegister)
                {
                    _commandNameRegister = true;
                    _command.PropertyChanged += CommandPropertyChanged;
                }
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("NetworkCommand"));
                }
            }
        }

        void CommandPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Name" | e.PropertyName == "Code")
            {
                CommandName = ((NetworkCommand)sender).Name;
            }

        }

        private string _commandName;
        public string CommandName
        {
            get { return _commandName; }
            set
            {
                _commandName = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("CommandName"));
                }
            }
        }
        
        public event PropertyChangedEventHandler PropertyChanged;
    }

    [DataContract]
    public class NetworkCommand : INotifyPropertyChanged
    {
        private string _name;
        [DataMember]
        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("Name"));
                    if (string.IsNullOrEmpty(_className))
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs("ClassName"));
                    }
                }
            }
        }
        private string _description;
        [DataMember]
        public string Description
        {
            get { return _description; }
            set
            {
                _description = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("Description"));
                }
            }

        }

        private int _code;
        [DataMember]
        [Range(0,0xffff)]
        public int Code
        {
            get { return _code; }
            set
            {
                _code = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("Code"));
                }
            }
        }

        private string _namespace;
        [DataMember]
        public string Namespace
        {
            get { return _namespace; }
            set
            {
                _namespace = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("Namespace"));
                }
            }
        }

        private string _className;
        [DataMember]
        public string ClassName
        {
            get
            {
                //
                    if (string.IsNullOrEmpty(_className))
                    {
                        return _name.Replace(" ", "").Replace("!", "").Replace(":", "").Replace(".", "");
                    }
                    return _name;
            }
            set
            {
                _className = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("ClassName"));
                }
            }
        }

        private Type _commandType;
        [DataMember]
        public Type CommandType
        {
            get { return _commandType; }
            set
            {
                _commandType = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("CommandType"));
                }
            }
        }

        private ObservableCollection<ClassProperty> _classProperties;
        [DataMember]
        public ObservableCollection<ClassProperty> ClassProperties
        {
            get { return _classProperties; }
            set
            {
                _classProperties = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("ClassProperties"));
                }
            }
        }

        private ObservableCollection<EnumerationDefinition> _enumerationList;
        [DataMember]
        public ObservableCollection<EnumerationDefinition> EnumerationList
        {
            get { return _enumerationList; }
            set
            {
                _enumerationList = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("EnumerationList"));
                }
            }
        }

        private EnumerationDefinition _selectedEnumeration;
        public EnumerationDefinition SelectedEnumeration
        {
            get { return _selectedEnumeration; }
            set
            {
                _selectedEnumeration = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("SelectedEnumeration"));
                }
            }
        }
        public ICollection<TypeMap> TypeMapping { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    [DataContract]
    public class TypeMap
    {
        public TypeMap() { }
        public TypeMap(string orig, string newty)
        {
            OriginalType = orig;
            NewType = newty;
        }
        public string Name
        {
            get
            {
                return string.Format("{0} is mapped to {1}", OriginalType, NewType);
            }
        }
        [DataMember]
        public string OriginalType { get; set; }
        [DataMember]
        public string NewType { get; set; }
    }

    [DataContract]
    public class EnumerationItem
    {
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public int Value { get; set; }
    }

    [DataContract]
    public class EnumerationDefinition : INotifyPropertyChanged
    {
        [DataMember]
        public string EnumerationName { get; set; }
        [DataMember]
        public ObservableCollection<EnumerationItem> Items { get; set; }

        private int _lastValue;
        public int LastValue
        {
            get
            {
                if (Items.Count <= 0) return _lastValue;

                foreach (var item in Items)
                {
                    _lastValue = Math.Max(item.Value, _lastValue);
                }

                if (Flags)
                {
                    if (_lastValue > 1)
                    {
                        _lastValue = (int)Math.Log((double)_lastValue, 2.0);
                    }
                }
                return _lastValue;

            }
            set { _lastValue = value; }
        }

        private bool _flags;
        public bool Flags
        {
            get { return _flags; }
            set
            {
                _flags = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("Flags"));
                }
            }
        }

        public void AddEnumerationItem(string itemName)
        {

            if (Items == null)
            {
                Items = new ObservableCollection<EnumerationItem>();
            }

            var value = LastValue;
            if (Flags && value >= 1)
            {
                ++value;
            }
            if (Flags)
            {
                value = 1 << value;
            }

            if (!Flags)
            {
                ++value;
            }

            Items.Add(new EnumerationItem() { Name = itemName, Value = value });
        }


        public override string ToString()
        {
            return EnumerationName;
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    [DataContract]
    public class ClassProperty
    {
        [DataMember]
        public string TypeName { get; set; }
        [DataMember]
        public string Name { get; set; }

        public override string ToString()
        {
            return string.Format("{0} {1}", TypeName, Name);
        }
    }
}
