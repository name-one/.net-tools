using System;
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

        [TestMethod]
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

        [TestMethod]
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

        //public virtual void GetRandomHumanViaOutput(out long id, out string firstName, out string lastName)
        //{
        //    System.Data.SqlClient.SqlParameter idSqlParameter = new System.Data.SqlClient.SqlParameter();
        //    idSqlParameter.ParameterName = "id";
        //    idSqlParameter.Value = System.DBNull.Value;
        //    idSqlParameter.Direction = System.Data.ParameterDirection.Output;
        //    idSqlParameter.Size = Int32.MaxValue;
        //    System.Data.SqlClient.SqlParameter firstNameSqlParameter = new System.Data.SqlClient.SqlParameter();
        //    firstNameSqlParameter.ParameterName = "firstName";
        //    firstNameSqlParameter.Value = DBNull.Value;
        //    firstNameSqlParameter.Direction = System.Data.ParameterDirection.Output;
        //    firstNameSqlParameter.Size = Int32.MaxValue;
        //    System.Data.SqlClient.SqlParameter lastNameSqlParameter = new System.Data.SqlClient.SqlParameter();
        //    lastNameSqlParameter.ParameterName = "lastName";
        //    lastNameSqlParameter.Value = DBNull.Value;
        //    lastNameSqlParameter.Direction = System.Data.ParameterDirection.Output;
        //    lastNameSqlParameter.Size = Int32.MaxValue;
        //    Context.Execute("EXEC GetRandomHumanViaOutput @id output,@firstName output,@lastName output", idSqlParameter, firstNameSqlParameter, lastNameSqlParameter);
        //    id = ((long)(idSqlParameter.Value));
        //    firstName = ((string)(firstNameSqlParameter.Value));
        //    lastName = ((string)(lastNameSqlParameter.Value));
        //}

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