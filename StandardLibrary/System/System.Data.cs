// CRAB Standard Library - System.Data.cs
// Database interfaces and data structures

namespace System.Data
{
    // ===== DATA TABLE =====
    
    public class DataTable
    {
        private string tableName;
        private DataColumnCollection columns;
        private DataRowCollection rows;
        
        public DataTable()
        {
            tableName = "";
            columns = new DataColumnCollection();
            rows = new DataRowCollection(this);
        }
        
        public DataTable(string tableName)
        {
            this.tableName = tableName ?? "";
            columns = new DataColumnCollection();
            rows = new DataRowCollection(this);
        }
        
        public string TableName
        {
            get { return tableName; }
            set { tableName = value ?? ""; }
        }
        
        public DataColumnCollection Columns
        {
            get { return columns; }
        }
        
        public DataRowCollection Rows
        {
            get { return rows; }
        }
        
        public DataRow NewRow()
        {
            return new DataRow(this);
        }
        
        public void Clear()
        {
            rows.Clear();
        }
    }
    
    // ===== DATA COLUMN =====
    
    public class DataColumn
    {
        private string columnName;
        private Type dataType;
        private bool allowNull;
        private object defaultValue;
        
        public DataColumn()
        {
            columnName = "";
            dataType = typeof(string);
            allowNull = true;
            defaultValue = null;
        }
        
        public DataColumn(string columnName)
        {
            this.columnName = columnName ?? "";
            dataType = typeof(string);
            allowNull = true;
            defaultValue = null;
        }
        
        public DataColumn(string columnName, Type dataType)
        {
            this.columnName = columnName ?? "";
            this.dataType = dataType ?? typeof(string);
            allowNull = true;
            defaultValue = null;
        }
        
        public string ColumnName
        {
            get { return columnName; }
            set { columnName = value ?? ""; }
        }
        
        public Type DataType
        {
            get { return dataType; }
            set { dataType = value ?? typeof(string); }
        }
        
        public bool AllowDBNull
        {
            get { return allowNull; }
            set { allowNull = value; }
        }
        
        public object DefaultValue
        {
            get { return defaultValue; }
            set { defaultValue = value; }
        }
    }
    
    // ===== DATA ROW =====
    
    public class DataRow
    {
        private DataTable table;
        private object[] itemArray;
        
        internal DataRow(DataTable table)
        {
            this.table = table;
            itemArray = new object[table.Columns.Count];
        }
        
        public DataTable Table
        {
            get { return table; }
        }
        
        public object this[int columnIndex]
        {
            get
            {
                if (columnIndex < 0 || columnIndex >= itemArray.Length)
                    throw new IndexOutOfRangeException();
                return itemArray[columnIndex];
            }
            set
            {
                if (columnIndex < 0 || columnIndex >= itemArray.Length)
                    throw new IndexOutOfRangeException();
                itemArray[columnIndex] = value;
            }
        }
        
        public object this[string columnName]
        {
            get
            {
                int index = table.Columns.IndexOf(columnName);
                if (index < 0)
                    throw new ArgumentException("Column not found: " + columnName);
                return itemArray[index];
            }
            set
            {
                int index = table.Columns.IndexOf(columnName);
                if (index < 0)
                    throw new ArgumentException("Column not found: " + columnName);
                itemArray[index] = value;
            }
        }
        
        public object[] ItemArray
        {
            get { return itemArray; }
            set
            {
                if (value != null && value.Length == itemArray.Length)
                    itemArray = value;
            }
        }
    }
    
    // ===== DATA COLUMN COLLECTION =====
    
    public class DataColumnCollection
    {
        private List<DataColumn> columns;
        
        internal DataColumnCollection()
        {
            columns = new List<DataColumn>();
        }
        
        public int Count
        {
            get { return columns.Count; }
        }
        
        public DataColumn this[int index]
        {
            get
            {
                if (index < 0 || index >= columns.Count)
                    throw new IndexOutOfRangeException();
                return columns[index];
            }
        }
        
        public DataColumn this[string name]
        {
            get
            {
                for (int i = 0; i < columns.Count; i++)
                {
                    if (columns[i].ColumnName == name)
                        return columns[i];
                }
                return null;
            }
        }
        
        public void Add(DataColumn column)
        {
            if (column == null)
                throw new ArgumentNullException("column");
            columns.Add(column);
        }
        
        public DataColumn Add(string columnName)
        {
            DataColumn column = new DataColumn(columnName);
            columns.Add(column);
            return column;
        }
        
        public DataColumn Add(string columnName, Type type)
        {
            DataColumn column = new DataColumn(columnName, type);
            columns.Add(column);
            return column;
        }
        
        public void Remove(DataColumn column)
        {
            columns.Remove(column);
        }
        
        public void RemoveAt(int index)
        {
            columns.RemoveAt(index);
        }
        
        public void Clear()
        {
            columns.Clear();
        }
        
        public bool Contains(string name)
        {
            return IndexOf(name) >= 0;
        }
        
        public int IndexOf(string columnName)
        {
            for (int i = 0; i < columns.Count; i++)
            {
                if (columns[i].ColumnName == columnName)
                    return i;
            }
            return -1;
        }
    }
    
    // ===== DATA ROW COLLECTION =====
    
    public class DataRowCollection
    {
        private DataTable table;
        private List<DataRow> rows;
        
        internal DataRowCollection(DataTable table)
        {
            this.table = table;
            rows = new List<DataRow>();
        }
        
        public int Count
        {
            get { return rows.Count; }
        }
        
        public DataRow this[int index]
        {
            get
            {
                if (index < 0 || index >= rows.Count)
                    throw new IndexOutOfRangeException();
                return rows[index];
            }
        }
        
        public void Add(DataRow row)
        {
            if (row == null)
                throw new ArgumentNullException("row");
            if (row.Table != table)
                throw new ArgumentException("Row belongs to another table");
            rows.Add(row);
        }
        
        public DataRow Add(params object[] values)
        {
            DataRow row = table.NewRow();
            if (values != null && values.Length > 0)
            {
                int count = Math.Min(values.Length, table.Columns.Count);
                for (int i = 0; i < count; i++)
                    row[i] = values[i];
            }
            rows.Add(row);
            return row;
        }
        
        public void Remove(DataRow row)
        {
            rows.Remove(row);
        }
        
        public void RemoveAt(int index)
        {
            rows.RemoveAt(index);
        }
        
        public void Clear()
        {
            rows.Clear();
        }
    }
    
    // ===== DATA SET =====
    
    public class DataSet
    {
        private string dataSetName;
        private DataTableCollection tables;
        
        public DataSet()
        {
            dataSetName = "NewDataSet";
            tables = new DataTableCollection();
        }
        
        public DataSet(string dataSetName)
        {
            this.dataSetName = dataSetName ?? "NewDataSet";
            tables = new DataTableCollection();
        }
        
        public string DataSetName
        {
            get { return dataSetName; }
            set { dataSetName = value ?? ""; }
        }
        
        public DataTableCollection Tables
        {
            get { return tables; }
        }
        
        public void Clear()
        {
            foreach (DataTable table in tables)
            {
                table.Clear();
            }
        }
    }
    
    // ===== DATA TABLE COLLECTION =====
    
    public class DataTableCollection : IEnumerable<DataTable>
    {
        private List<DataTable> tables;
        
        internal DataTableCollection()
        {
            tables = new List<DataTable>();
        }
        
        public int Count
        {
            get { return tables.Count; }
        }
        
        public DataTable this[int index]
        {
            get
            {
                if (index < 0 || index >= tables.Count)
                    throw new IndexOutOfRangeException();
                return tables[index];
            }
        }
        
        public DataTable this[string name]
        {
            get
            {
                for (int i = 0; i < tables.Count; i++)
                {
                    if (tables[i].TableName == name)
                        return tables[i];
                }
                return null;
            }
        }
        
        public void Add(DataTable table)
        {
            if (table == null)
                throw new ArgumentNullException("table");
            tables.Add(table);
        }
        
        public DataTable Add(string name)
        {
            DataTable table = new DataTable(name);
            tables.Add(table);
            return table;
        }
        
        public void Remove(DataTable table)
        {
            tables.Remove(table);
        }
        
        public void RemoveAt(int index)
        {
            tables.RemoveAt(index);
        }
        
        public void Clear()
        {
            tables.Clear();
        }
        
        public bool Contains(string name)
        {
            return this[name] != null;
        }
        
        public IEnumerator<DataTable> GetEnumerator()
        {
            return tables.GetEnumerator();
        }
    }
    
    // ===== DATABASE CONNECTION INTERFACES =====
    
    public interface IDbConnection : IDisposable
    {
        string ConnectionString { get; set; }
        int ConnectionTimeout { get; }
        string Database { get; }
        ConnectionState State { get; }
        void Open();
        void Close();
        IDbCommand CreateCommand();
    }
    
    public interface IDbCommand : IDisposable
    {
        string CommandText { get; set; }
        int CommandTimeout { get; set; }
        CommandType CommandType { get; set; }
        IDbConnection Connection { get; set; }
        IDataParameterCollection Parameters { get; }
        IDbDataParameter CreateParameter();
        int ExecuteNonQuery();
        IDataReader ExecuteReader();
        object ExecuteScalar();
    }
    
    public interface IDataReader : IDisposable
    {
        int FieldCount { get; }
        bool IsClosed { get; }
        int RecordsAffected { get; }
        object this[int index] { get; }
        object this[string name] { get; }
        void Close();
        bool Read();
        bool NextResult();
        string GetName(int index);
        int GetOrdinal(string name);
        bool GetBoolean(int index);
        byte GetByte(int index);
        long GetBytes(int index, long fieldOffset, byte[] buffer, int bufferoffset, int length);
        char GetChar(int index);
        long GetChars(int index, long fieldoffset, char[] buffer, int bufferoffset, int length);
        string GetDataTypeName(int index);
        DateTime GetDateTime(int index);
        decimal GetDecimal(int index);
        double GetDouble(int index);
        Type GetFieldType(int index);
        float GetFloat(int index);
        Guid GetGuid(int index);
        short GetInt16(int index);
        int GetInt32(int index);
        long GetInt64(int index);
        string GetString(int index);
        object GetValue(int index);
        int GetValues(object[] values);
        bool IsDBNull(int index);
    }
    
    public interface IDbDataParameter
    {
        DbType DbType { get; set; }
        ParameterDirection Direction { get; set; }
        bool IsNullable { get; }
        string ParameterName { get; set; }
        string SourceColumn { get; set; }
        DataRowVersion SourceVersion { get; set; }
        object Value { get; set; }
        byte Precision { get; set; }
        byte Scale { get; set; }
        int Size { get; set; }
    }
    
    public interface IDataParameterCollection
    {
        object this[string parameterName] { get; set; }
        int Count { get; }
        void Add(object value);
        void Clear();
        bool Contains(string parameterName);
        int IndexOf(string parameterName);
        void RemoveAt(string parameterName);
    }
    
    // ===== ENUMS =====
    
    public enum ConnectionState
    {
        Closed = 0,
        Open = 1,
        Connecting = 2,
        Executing = 4,
        Fetching = 8,
        Broken = 16
    }
    
    public enum CommandType
    {
        Text = 1,
        StoredProcedure = 4,
        TableDirect = 512
    }
    
    public enum DbType
    {
        AnsiString = 0,
        Binary = 1,
        Byte = 2,
        Boolean = 3,
        Currency = 4,
        Date = 5,
        DateTime = 6,
        Decimal = 7,
        Double = 8,
        Guid = 9,
        Int16 = 10,
        Int32 = 11,
        Int64 = 12,
        Object = 13,
        SByte = 14,
        Single = 15,
        String = 16,
        Time = 17,
        UInt16 = 18,
        UInt32 = 19,
        UInt64 = 20,
        VarNumeric = 21,
        AnsiStringFixedLength = 22,
        StringFixedLength = 23,
        Xml = 25,
        DateTime2 = 26,
        DateTimeOffset = 27
    }
    
    public enum ParameterDirection
    {
        Input = 1,
        Output = 2,
        InputOutput = 3,
        ReturnValue = 6
    }
    
    public enum DataRowVersion
    {
        Original = 256,
        Current = 512,
        Proposed = 1024,
        Default = 1536
    }
    
    // ===== SIMPLE DB CONNECTION (ABSTRACT) =====
    
    public abstract class DbConnection : IDbConnection
    {
        protected string connectionString;
        protected ConnectionState state;
        
        protected DbConnection()
        {
            connectionString = "";
            state = ConnectionState.Closed;
        }
        
        public string ConnectionString
        {
            get { return connectionString; }
            set { connectionString = value ?? ""; }
        }
        
        public abstract int ConnectionTimeout { get; }
        public abstract string Database { get; }
        
        public ConnectionState State
        {
            get { return state; }
        }
        
        public abstract void Open();
        public abstract void Close();
        public abstract IDbCommand CreateCommand();
        
        public virtual void Dispose()
        {
            Close();
        }
    }
    
    // ===== SIMPLE DB COMMAND (ABSTRACT) =====
    
    public abstract class DbCommand : IDbCommand
    {
        protected string commandText;
        protected int commandTimeout;
        protected CommandType commandType;
        protected IDbConnection connection;
        protected IDataParameterCollection parameters;
        
        protected DbCommand()
        {
            commandText = "";
            commandTimeout = 30;
            commandType = CommandType.Text;
        }
        
        public string CommandText
        {
            get { return commandText; }
            set { commandText = value ?? ""; }
        }
        
        public int CommandTimeout
        {
            get { return commandTimeout; }
            set { commandTimeout = value; }
        }
        
        public CommandType CommandType
        {
            get { return commandType; }
            set { commandType = value; }
        }
        
        public IDbConnection Connection
        {
            get { return connection; }
            set { connection = value; }
        }
        
        public IDataParameterCollection Parameters
        {
            get { return parameters; }
        }
        
        public abstract IDbDataParameter CreateParameter();
        public abstract int ExecuteNonQuery();
        public abstract IDataReader ExecuteReader();
        public abstract object ExecuteScalar();
        
        public virtual void Dispose()
        {
            // Cleanup
        }
    }
    
    // ===== SQL EXCEPTIONS =====
    
    public class DataException : Exception
    {
        public DataException() : base("A data error occurred")
        {
        }
        
        public DataException(string message) : base(message)
        {
        }
        
        public DataException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
    
    public class ConstraintException : DataException
    {
        public ConstraintException() : base("A constraint was violated")
        {
        }
        
        public ConstraintException(string message) : base(message)
        {
        }
        
        public ConstraintException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
    
    public class DBConcurrencyException : DataException
    {
        public DBConcurrencyException() : base("A concurrency violation occurred")
        {
        }
        
        public DBConcurrencyException(string message) : base(message)
        {
        }
        
        public DBConcurrencyException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
