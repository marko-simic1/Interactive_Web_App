using System;
using System.Text;

namespace RPPP_WebApp.Extensions
{
  public static class ExceptionExtensions
  {
    public static string CompleteExceptionMessage(this Exception exc)
    {
      StringBuilder sb = new StringBuilder();
      while (exc != null)
      {
        sb.AppendLine(exc.Message);
        exc = exc.InnerException;
      }
      return sb.ToString();
    }
  }
}
