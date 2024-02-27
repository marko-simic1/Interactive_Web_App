using Microsoft.AspNetCore.Mvc;
using RPPP_WebAp.models.JTable;
using RPPP_WebApp.ViewModels;
using System.Threading.Tasks;

namespace RPPP_WebApp.Controllers.JTable
{
    [Route("jtable/zadatak/[action]")]
    public class ZadatakJTableController : JTableController<JZadatakController, int, JZadatakViewModel>
    {
        public ZadatakJTableController(JZadatakController controller) : base(controller)
        {

        }

        [HttpPost]
        public async Task<JTableAjaxResult> Update([FromForm] JZadatakViewModel model)
        {
            return await base.UpdateItem(model.IdZadatka, model);
        }

        [HttpPost]
        public async Task<JTableAjaxResult> Delete([FromForm] int IdZadatka)
        {
            return await base.DeleteItem(IdZadatka);
        }
    }
}