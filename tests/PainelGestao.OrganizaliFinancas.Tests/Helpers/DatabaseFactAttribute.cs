namespace PainelGestao.OrganizaliFinancas.Tests.Helpers
{
    /// <summary>Database writes require an explicit opt-in; default test runs remain read-only.</summary>
    public sealed class DatabaseFactAttribute : FactAttribute
    {
        public DatabaseFactAttribute()
        {
            if (Environment.GetEnvironmentVariable("ORGANIZALI_DATABASE_TESTS") != "1")
                Skip = "Requires explicit authorization for writes in a disposable MariaDB container.";
        }
    }
}
