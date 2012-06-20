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
                InsertIntoHuman(context, item);
            }

            var resultHumans = context.Procedures.GetHumans();
            Assert.IsTrue(testHumans.ElementwiseEquals(resultHumans));
        }

        [TestMethod]
        public void GetHumanById()
        {
            var context = CreateSqlContext();

            context.Execute("DELETE Human WHERE Id = 100 OR Id = 101");
            Human testHuman = new Human { Id = 100, FirstName = "Josef", LastName = "Kobzon" };
            InsertIntoHuman(context, testHuman);

            Human resultHuman = context.Procedures.GetHumanById(100);
            Assert.IsTrue(testHuman.MemberwiseEquals(resultHuman));

            resultHuman = context.Procedures.GetHumanById(101);
            Assert.IsNull(resultHuman);
        }

        [TestMethod]
        public void Nulls()
        {
            var context = CreateSqlContext();

            Human[] testHumans = new Human[]
            {
                new Human{Id = null, FirstName = "Josef", LastName = "Kobzon"},
                new Human{Id = 2, FirstName = null, LastName = "Rotaru"},
                new Human{Id = 3, FirstName = "Larisa", LastName = null}
            };
            context.Execute("TRUNCATE TABLE Human");
            foreach (var item in testHumans)
            {
                context.Procedures.AddHuman(item.Id, item.FirstName, item.LastName);
            }

            var resultHumans = context.Execute<Human>("SELECT * FROM Human");

            Assert.IsTrue(testHumans.ElementwiseEquals(resultHumans));
        }

        private void InsertIntoHuman(SqlContext context, Human human)
        {
            context.Execute("INSERT INTO Human VALUES(@id, @firstName, @lastName)",
                    new SqlParameter("id", human.Id),
                    new SqlParameter("firstName", human.FirstName),
                    new SqlParameter("lastName", human.LastName));
        }

        private SqlContext<IProceduresProxy> CreateSqlContext()
        {
            return new SqlContext<IProceduresProxy>("data source=(local);initial catalog=InoSoft.Tools.Data.Test;integrated security=true");
        }
    }
}