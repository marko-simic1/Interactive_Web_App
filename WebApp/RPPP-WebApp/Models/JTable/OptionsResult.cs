
﻿using RPPP_WebAp.models.JTable;
using System.Collections.Generic;


namespace RPPP_WebApp.Models.JTable
{
  public class OptionsResult : JTableAjaxResult
  {
    public OptionsResult(List<TextValue> options)
    {
      Options = options;
    }
    public List<TextValue> Options { get; set; }
  }
}
