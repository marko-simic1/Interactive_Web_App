using RPPP_WebAp.models.JTable;
namespace RPPP_WebApp.Models.JTable

{
  public class CreateResult : JTableAjaxResult
  {
    public CreateResult(object record) : base()
    {
      Record = record;
    }    
    public object Record { get; set; }
  }
}
