using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using RPPP_WebApp.Models;
using RPPP_WebApp.Models.JTable;
using RPPP_WebApp.ViewModels;
using RPPP_WebAp.models.JTable;

namespace RPPP_WebApp.Controllers.JTable
{
  [Route("jtable/uloga/[action]")]  
  public class MjestoJTableController : JTableController<UlogaApiController, int, UlogeViewModel>
  {
    public MjestoJTableController(UlogaApiController controller) : base(controller)
    {

    }

    [HttpPost]
    public async Task<JTableAjaxResult> Update([FromForm] UlogeViewModel model)
    {
      return await base.UpdateItem(model.IdUloge, model);
    }

    [HttpPost]
    public async Task<JTableAjaxResult> Delete([FromForm] int IdUloge)
    {
      return await base.DeleteItem(IdUloge);
    }
  }
}