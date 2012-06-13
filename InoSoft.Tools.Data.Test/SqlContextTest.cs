using System.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace InoSoft.Tools.Data.Test
{
    [TestClass]
    public class SqlContextTest
    {
        [TestMethod]
        public void GetHumans()
        {
            var context = CreateSqlContext();

            Human[] testHumans = new Human[]
            {
                new Human{Id = 1, FirstName = "Josef", LastName = "Kobzon"},
                new Human{Id = 2, FirstName = "Sofia", LastName = "Rotaru"},
                new Human{Id = 3, FirstName = "Larisa", LastName = "Dolina"}
            };

            context.Execute("TRUNCATE TABLE Human");
            foreach (var item in testHumans)
            {
                context.Execute("INSERT INTO Human VALUES(@id, @firstName, @lastName)",
                    new SqlParameter("id", item.Id),
                    new SqlParameter("firstName", item.FirstName),
                    new SqlParameter("lastName", item.LastName));
            }

            var resultHumans = context.ProceduresProxy.GetHumans();
            Assert.IsTrue(testHumans.ElementwiseEquals(resultHumans));
        }

        private SqlContext<IProceduresProxy> CreateSqlContext()
        {
            return new SqlContext<IProceduresProxy>("data source=(local);initial catalog=InoSoft.Tools.Data.Test;integrated security=true");
        }
    }
}