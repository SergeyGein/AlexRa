using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Front.Domain.Abstract;
using Front.Domain.Entites;
using Front.Domain.Concrete;
using Front.WebUI.Models.Game;
using System.Globalization;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.AspNet.Identity;
using Front.WebUI.Models;

namespace Front.WebUI.Controllers
{
    public class GameController : Controller
    {
        private ApplicationUserManager UserManager
        {
            get
            {
                return HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
        }
        private IUnitRepository repository;
        public GameController(IUnitRepository productRepository)
        {
            this.repository = productRepository;
        }

        // GET: Game
        [Authorize]
        public ActionResult Index()
        {
            return View();
        }
        public JsonResult GetData()
        {
            ApplicationUser user = UserManager.FindByEmail(User.Identity.Name);
            UnitDataModel unitsDataModel = new UnitDataModel();
            var wewr = repository.Units.ToList();
            unitsDataModel.AllUnits = repository.Units.GroupBy(a => new { a.X, a.Y, a.Type, a.Land }, (key, group) => new UnitsByTypes(){
                X = key.X,
                Y = key.Y,
                Type = key.Type,
                Land = key.Land,
                Count = group.Count()
            }).ToList();
            unitsDataModel.MyUnits = repository.Units.Where(a => a.PlayerMail ==user.UserName).ToList();            
            return Json(unitsDataModel, JsonRequestBehavior.AllowGet);
        }
        public JsonResult PlayerTurn(int x, int y, int UnitId)
        {
            ApplicationUser user = UserManager.FindByEmail(User.Identity.Name);
            ResultTurn resultTurn = repository.Turn( user.UserName,  user.Name,  UnitId, x, y);
            return Json(resultTurn, JsonRequestBehavior.AllowGet);
        }

            
        
    }
}