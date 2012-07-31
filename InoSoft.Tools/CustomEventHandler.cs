namespace InoSoft.Tools
{
    public delegate void CustomEventHandler<in TSender, in TArg>(TSender sender, TArg arg);

    public static class CustomEventHandlerExtensions
    {
        public static void Raise<TSender, TArgs>(this CustomEventHandler<TSender, TArgs> eventHandler, TSender sender, TArgs args)
        {
            if (eventHandler != null)
            {
                eventHandler(sender, args);
            }
        }
    }
}