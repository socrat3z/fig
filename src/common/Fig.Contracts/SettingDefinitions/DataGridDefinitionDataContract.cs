using System.Collections.Generic;

namespace Fig.Contracts.SettingDefinitions
{
    public class DataGridDefinitionDataContract
    {
        public DataGridDefinitionDataContract(List<DataGridColumnDataContract> columns, bool isLocked, bool isDictionary = false)
        {
            Columns = columns;
            IsLocked = isLocked;
            IsDictionary = isDictionary;
        }

        public List<DataGridColumnDataContract> Columns { get; }

        public bool IsLocked { get; set; }

        /// <summary>
        /// True when this DataGrid represents a Dictionary&lt;string, T&gt; setting.
        /// The first column ("Key") holds the dictionary key; remaining columns are the value type's properties.
        /// </summary>
        public bool IsDictionary { get; set; }
    }
}