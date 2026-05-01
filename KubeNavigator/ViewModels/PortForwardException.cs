using System;

namespace KubeNavigator.ViewModels;

public class PortForwardException : Exception
{
    public PortForwardException(string message)
        : base(message) { }

    public PortForwardException(string message, Exception innerException)
        : base(message, innerException) { }
}
