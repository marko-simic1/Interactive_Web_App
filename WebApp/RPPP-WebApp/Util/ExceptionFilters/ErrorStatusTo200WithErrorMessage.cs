﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using RPPP_WebAp.models.JTable;
using RPPP_WebApp.Models.JTable;
using RPPP_WebApp.Util.Extensions;

namespace WebServices.Util.ExceptionFilters
{
  public class ErrorStatusTo200WithErrorMessage : ExceptionFilterAttribute
  {
    private readonly ILogger<ErrorStatusTo200WithErrorMessage> logger;

    public ErrorStatusTo200WithErrorMessage(ILogger<ErrorStatusTo200WithErrorMessage> logger)
    {
      this.logger = logger;
    }

    public override void OnException(ExceptionContext context)
    {      
      string exceptionMessage = context.Exception.CompleteExceptionMessage();
      context.ExceptionHandled = true;      
      JTableAjaxResult result = JTableAjaxResult.Error(exceptionMessage);
      context.Result = new OkObjectResult(result);           
    }
  }
}
