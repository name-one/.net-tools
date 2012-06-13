namespace InoSoft.Tools.Data.Test
{
    public interface IProceduresProxy
    {
        Human[] GetHumans();

        int GetHumansCount();

        Human GetHumanById(long id);
    }
}