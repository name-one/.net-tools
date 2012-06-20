namespace InoSoft.Tools.Data.Test
{
    public interface IProceduresProxy
    {
        Human[] GetHumans();

        int GetHumansCount();

        void AddHuman(long? id, string firstName, string lastName);

        Human GetHumanById(long id);
    }
}