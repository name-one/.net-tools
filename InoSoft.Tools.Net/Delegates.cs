using System;

namespace InoSoft.Tools.Net
{
    /// <summary>
    /// Defines event handler for async exceptions.
    /// </summary>
    /// <param name="ex">Caught exception.</param>
    public delegate void ExceptionHandler(Exception ex);

    /// <summary>
    /// Defines event handler for async connection related exceptions.
    /// </summary>
    /// <param name="connection">Connection, which is related to caught exception.</param>
    /// <param name="ex">Caught exception.</param>
    public delegate void ConnectionExceptionHandler(Connection connection, Exception ex);
}