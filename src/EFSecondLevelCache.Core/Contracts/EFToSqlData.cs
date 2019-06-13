namespace EFSecondLevelCache.Core.Contracts
{
    /// <summary>
    /// Represents the computed Sql info
    /// </summary>
    public class EFToSqlData
    {
        /// <summary>
        /// The computed Sql
        /// </summary>
        public string Sql { set; get; }

        /// <summary>
        /// Expression's Hash
        /// </summary>
        public string ExpressionKeyHash { set; get; }

        /// <summary>
        /// Represents the computed Sql info
        /// </summary>
        public EFToSqlData() { }

        /// <summary>
        /// Represents the computed Sql info
        /// </summary>
        public EFToSqlData(string sql, string expressionKeyHash)
        {
            Sql = sql;
            ExpressionKeyHash = expressionKeyHash;
        }
    }
}