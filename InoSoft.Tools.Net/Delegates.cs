using System;

namespace InoSoft.Tools.Net
{
    public delegate void ExceptionHandler(Exception ex);

    public delegate void ClientExceptionHandler(Connection client, Exception ex);
}