using System;
using System.Data.SqlClient;
using System.Text;
using NUnit.Framework;

namespace InoSoft.Tools.Data.Test
{
    [TestFixture]
    public class SqlContextTest
    {
        [Test]
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

        [Test]
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

        [Test]
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

        [Test]
        public void StringOutputs()
        {
            var context = CreateSqlContext();

            Human testHuman = new Human { Id = 100, FirstName = "Josef", LastName = "Kobzon" };
            context.Execute("TRUNCATE TABLE Human");
            InsertIntoHuman(context, testHuman);

            string firstName, lastName;
            context.Procedures.GetHumanViaOutput(100, out firstName, out lastName);
            Assert.AreEqual(firstName, testHuman.FirstName);
            Assert.AreEqual(lastName, testHuman.LastName);
        }

        [Test]
        public void VariousOutputs()
        {
            var context = CreateSqlContext();

            Human testHuman = new Human { Id = 100, FirstName = "Josef", LastName = "Kobzon" };
            context.Execute("TRUNCATE TABLE Human");
            InsertIntoHuman(context, testHuman);

            long id;
            string firstName, lastName;
            context.Procedures.GetRandomHumanViaOutput(out id, out firstName, out lastName);
            Assert.AreEqual(id, testHuman.Id);
            Assert.AreEqual(firstName, testHuman.FirstName);
            Assert.AreEqual(lastName, testHuman.LastName);
        }

        [Test]
        public void Enum()
        {
            var context = CreateSqlContext();

            Human[] testHumans = new Human[]
            {
                new Human{Id = null, FirstName = "Josef", LastName = "Kobzon"},
                new Human{Id = 2, FirstName = "Sofia", LastName = "Rotaru"},
                new Human{Id = 3, FirstName = "Larisa", LastName = "Dolina"}
            };
            HumanId?[] testHumanIds = new HumanId?[]
            {
                null,
                HumanId.Rotaru,
                HumanId.Dolina
            };
            context.Execute("TRUNCATE TABLE Human");
            foreach (var item in testHumans)
            {
                InsertIntoHuman(context, item);
            }

            for (int i = 0; i < 3; i++)
            {
                var resultHuman = context.Procedures.GetHumanById(testHumanIds[i]);
                Assert.IsTrue(testHumans[i].MemberwiseEquals(resultHuman));
                if (testHumanIds[i].HasValue)
                {
                    resultHuman = context.Procedures.GetHumanById(testHumanIds[i].Value);
                    Assert.IsTrue(testHumans[i].MemberwiseEquals(resultHuman));
                }
            }
        }

        [Test]
        public void ProcessText()
        {
            var context = CreateSqlContext();
            var sb = new StringBuilder();
            for (int i = 0; i < 100; i++)
            {
                sb.Append("abcd");
            }
            var testText = sb.ToString();
            var resultText = context.Procedures.ProcessText(testText);
            Assert.AreEqual(testText, resultText);
        }

        private void InsertIntoHuman(SqlContext context, Human human)
        {
            context.Execute("INSERT INTO Human VALUES(@id, @firstName, @lastName)",
                new SqlParameter("id", human.Id.HasValue ? (object)human.Id.Value : DBNull.Value),
                new SqlParameter("firstName", human.FirstName),
                new SqlParameter("lastName", human.LastName));
        }

        private SqlContext<IProceduresProxy> CreateSqlContext()
        {
            return new SqlContext<IProceduresProxy>("data source=.\\sqlexpress;initial catalog=InoSoft.Tools.Data.Test;integrated security=true");
        }
    }
}