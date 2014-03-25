using System.Data.SqlClient;

using Aspectacular;

namespace Example.AdventureWorks2008ObjectContext_Dal.Ado.net 
{
    public partial class AwDataSet 
    {
    }
}

namespace Example.AdventureWorks2008ObjectContext_Dal.Ado.net.AwDataSetTableAdapters 
{
    [InvariantReturn]
    public partial class ProductCategoryTableAdapter : ISqlServerConnectionProvider
    {
        SqlConnection ISqlServerConnectionProvider.SqlConnection
        {
            get { return this.Connection; }
        }
    }
}
