using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace Value.Framework.Core.Data
{
    /// <summary>
    /// A better (IEnumerable[IDataRecord]) version of executing and iteration through the ADO.NET data reader.
    /// </summary>
    public class EnumerableDataReader : IEnumerable<IDataRecord>, IEnumerator<IDataRecord>
    {
        protected readonly IDbCommand dbCommand;
        protected IDataReader dbReader = null;

        public EnumerableDataReader(IDbCommand cmd, CommandBehavior cmdBehavior = CommandBehavior.Default)
        {
            this.dbCommand = cmd;
            this.CommandBehavior = cmdBehavior;
        }

        public CommandBehavior CommandBehavior { get; set; }

        #region Implementation of IEnumerable<>

        public IEnumerator<IDataRecord> GetEnumerator()
        {
            return this;
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this;
        }

        #endregion Implementation of IEnumerable<>

        #region Implementation of IEnumerator<>

        public IDataRecord Current
        {
            get { return this.dbReader; }
        }

        public void Dispose()
        {
            if (this.dbReader != null)
            {
                this.dbReader.Close();
                this.dbReader = null;
            }
        }

        object System.Collections.IEnumerator.Current
        {
            get { return this.dbReader; }
        }

        public bool MoveNext()
        {
            if (this.dbReader == null)
                this.Reset();

            bool wasRead = this.dbReader.Read();
            return wasRead;
        }

        public void Reset()
        {
            if (this.dbReader != null)
                this.Dispose();

            this.dbReader = this.dbCommand.ExecuteReader(this.CommandBehavior);
        }

        #endregion Implementation of IEnumerator<>
    }

    /// <summary>
    /// A better (IEnumerable[TEntity]) version of executing and iteration through the ADO.NET data reader.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public class EnumerableDataReader<TEntity> : EnumerableDataReader, IEnumerable<TEntity>, IEnumerator<TEntity>
    {
        protected readonly Func<IDataRecord, TEntity> readerEntityMapper;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="readerEntityMapper">Method converting reader's IDataRecord into an entity</param>
        /// <param name="cmdBehavior"></param>
        public EnumerableDataReader(IDbCommand cmd, Func<IDataRecord, TEntity> readerEntityMapper, CommandBehavior cmdBehavior = CommandBehavior.Default)
            : base(cmd, cmdBehavior)
        {
            if (readerEntityMapper == null)
                throw new ArgumentNullException("readerEntityMapper");

            this.readerEntityMapper = readerEntityMapper;
        }

        #region Implementation of IEnumerable<>

        public new IEnumerator<TEntity> GetEnumerator()
        {
            return this;
        }

        #endregion Implementation of IEnumerable<>

        #region Implementation of IEnumerator<>

        public new TEntity Current
        {
            get { return this.readerEntityMapper(this.dbReader); }
        }

        #endregion Implementation of IEnumerator<>
    }

    public static partial class AdoNetExtensions
    {
        /// <summary>
        /// Returns enumerator for an ADO.NET data reader.
        /// </summary>
        /// <param name="dbCommand"></param>
        /// <param name="cmdBehavior"></param>
        /// <returns></returns>
        public static EnumerableDataReader GetEnumerableReader(this IDbCommand dbCommand, CommandBehavior cmdBehavior = CommandBehavior.Default)
        {
            return new EnumerableDataReader(dbCommand, cmdBehavior);
        }

        /// <summary>
        /// Returns strongly-typed enumerator for an ADO.NET data reader.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="dbCommand"></param>
        /// <param name="readerEntityMapper">Method converting reader's IDataRecord into an entity</param>
        /// <param name="cmdBehavior"></param>
        /// <returns></returns>
        public static EnumerableDataReader<TEntity> GetEnumerableReader<TEntity>(this IDbCommand dbCommand, Func<IDataRecord, TEntity> readerEntityMapper, CommandBehavior cmdBehavior = CommandBehavior.Default)
        {
            return new EnumerableDataReader<TEntity>(dbCommand, readerEntityMapper, cmdBehavior);
        }

        /// <summary>
        /// Exposes ADO.NET data reader as IEnumerable[IDataRecord].
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static IEnumerable<IDataRecord> AsEnumerable(this IDataReader reader)
        {
            while (reader.Read())
            {
                yield return reader;
            }
        }

        /// <summary>
        /// Exposes ADO.NET data reader as IEnumerable[TEntity].
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="reader"></param>
        /// <param name="readerEntityMapper">Method converting reader's IDataRecord into an entity</param>
        /// <returns></returns>
        public static IEnumerable<TEntity> AsEnumerable<TEntity>(this IDataReader reader, Func<IDataRecord, TEntity> readerEntityMapper)
        {
            while (reader.Read())
            {
                yield return readerEntityMapper(reader);
            }
        }

        /// <summary>
        /// Exposes ADO.NET DataTable as an IEnumerable[DataRow]
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        public static IEnumerable<DataRow> AsEnumerable(this DataTable table)
        {
            var stronglyTyped = table.DefaultView.Cast<DataRow>();
            return stronglyTyped;
        }

        /// <summary>
        /// </summary>
        /// <typeparam name="TDataRow">Strongly-typed subclass of DataRow.</typeparam>
        /// <param name="table"></param>
        /// <returns></returns>
        public static IEnumerable<TDataRow> AsEnumerable<TDataRow>(this DataTable table) where TDataRow : DataRow
        {
            var stronglyTyped = table.DefaultView.Cast<TDataRow>();
            return stronglyTyped;
        }
    }
}
