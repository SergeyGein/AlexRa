using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Front.Models.Game;
using Microsoft.AspNet.Identity.Owin;
using Front.Models;
using Microsoft.AspNet.Identity;
using System.Globalization;

namespace Front.Controllers
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
        // GET: Game
        [Authorize]
        public ActionResult Index()
        {
            ApplicationUser user = UserManager.FindByEmail(User.Identity.Name);         
            GameContext GC = new GameContext();
            GameUnitModel gameunitModel = new GameUnitModel();
             var obj = GC.Units.Where(q=>(q.Land ==user.Land && q.Reserve==false && q.Way <DateTime.Now) || (q.Visible == true)).GroupBy(a => new { a.coordinate, a.Type }, (key, group) => new {
                Coordinate = key.coordinate,
                Type = key.Type,
                Count = group.Count(),
                Land = group.FirstOrDefault().Land 
            }).ToList();
            gameunitModel.Map = obj.Select(o => new UnitMaps { Coordinate = o.Coordinate, Land = o.Land, Type = o.Type, Count = o.Count }).ToList();
            gameunitModel.MyUnits = GC.Units.Where(a => a.PlayerId == user.Email).OrderBy(s => s.Type).ToList();
           
            return View(gameunitModel);
        }

        public JsonResult PlayerTurn(int x, int y, int Id)
        {
            ApplicationUser user = UserManager.FindByEmail(User.Identity.Name);
            var result = "";
            GameContext Gc = new GameContext();
            Unit unit = Gc.Units.Where(a => a.UnitId == Id).FirstOrDefault();// Аттакующий юнит
            if(user.Email ==unit.PlayerId)
            {
                if ((unit.turn.Day + 1) < DateTime.Now.Ticks)
                {
                    int xx = int.Parse(unit.coordinate.Substring(0, 4)) - 1000;
                    int yy = int.Parse(unit.coordinate.Substring(5)) - 1000;
                    if ((Math.Abs(x - xx) < 2) && (Math.Abs(y - yy) < 2))
                    {
                        Unit u = Gc.Units.Where(a => a.coordinate == "10" + x + "x10" + y).OrderBy(b=>b.HP).FirstOrDefault();//Защищающийся юнит
                        string message = "";
                        if (u == null)
                        {
                            unit.coordinate = "10" + x + "x10" + y;
                            unit.turn = DateTime.Now;
                            Gc.Entry(unit).State = System.Data.Entity.EntityState.Modified;
                            Gc.SaveChanges();
                             message = user.Land + " произвёл захват области " + unit.coordinate + ", Игрок " + user.Name;
                            Journal ju = new Journal(user.Email, message, false);
                            Gc.Journals.Add(ju);
                            Gc.SaveChanges();
                            return Json(unit, JsonRequestBehavior.AllowGet);
                        }
                        if(u.Land == user.Land)
                        {
                            unit.coordinate = "10" + x + "x10" + y;
                            unit.turn = DateTime.Now;
                            Gc.Entry(unit).State = System.Data.Entity.EntityState.Modified;
                            Gc.SaveChanges();
                            message = user.Land + " произвёл перемещение в область " + unit.coordinate + ", Игрок " + user.Name;
                         }
                        else
                        {
                            if (unit.Ammunition < 1)
                            {
                                //Необходимо прописать ошибку. Нет патронов.
                                return Json(unit, JsonRequestBehavior.AllowGet);
                            }
                            Battle batle = new Battle(unit, u);
                            ResultBattle resultBattle = batle.Battle_Resume(Gc);
                            if(unit.Type== "Artillery") // Если ведём артобстрел
                            {
                                u.HP = u.HP - resultBattle.ResumeAttack;
                                unit.Ammunition = unit.Ammunition - 1;
                                message = user.Land + " произвёл артобстрел области " + unit.coordinate + ", отряд "+u.Class +" игрока "+ u.PlayerId+
                                    " получил повреждения -"+resultBattle.ResumeAttack+" ("+u.HP+")";
                                if (u.HP <0)
                                {
                                    //Прописываем уничтожение отряда
                                    Gc.Entry(u).State = System.Data.Entity.EntityState.Deleted;
                                    Gc.SaveChanges();
                                    message += message + ", отряд в результате  артобстрела был полностью уничтожен.";
                                }

                            }
                            else //Идём в ближний бой
                            {
                                u.HP = u.HP - resultBattle.ResumeAttack;
                                message= user.Land + " произвёл атаку обасти " + unit.coordinate + ", отряд " + u.Class + " игрока " + u.PlayerId +
                                    " получил повреждения -" + resultBattle.ResumeAttack + " (" + u.HP + ")";
                                if (u.Ammunition > 0)
                                {
                                    unit.HP = unit.HP - resultBattle.ResumeDefence;
                                    u.Ammunition = u.Ammunition - 1;
                                    message += message + ", атакующая сторона " + unit.Class + " понесла потери " + resultBattle.ResumeDefence;
                                    if( unit.HP <0)
                                    {
                                        //Уничтожается атакующий отряд
                                        Gc.Entry(unit).State = System.Data.Entity.EntityState.Deleted;
                                    }
                                }
                                unit.Ammunition = unit.Ammunition - 1;

                            }
                            unit.turn = DateTime.Now;                                                    
                            if (u.HP > 0)
                            {                                
                                Gc.Entry(u).State = System.Data.Entity.EntityState.Modified;                                                          
                            }
                            else
                            {
                                //Прописываем уничтожение отряда
                                Gc.Entry(u).State = System.Data.Entity.EntityState.Deleted;
                                unit.coordinate = "10" + x + "x10" + y;
                                
                                message += message + ", защищающийся отряд в результате атаки был полностью уничтожен.";
                            }
                            if (unit.HP > 0)
                            {
                                Gc.Entry(unit).State = System.Data.Entity.EntityState.Modified;
                            }
                            Gc.SaveChanges();
                        }
                        Journal j = new Journal(user.Email, message, false);
                        Gc.Journals.Add(j);
                        Gc.SaveChanges();
                        return Json(unit, JsonRequestBehavior.AllowGet);
                    }
                    else
                    {
                        result = "Неверно указаны координаты отряда";
                    }
                }
                else
                {
                    result = "Прошло меньше суток, данный отряд ещё не может производить действий!";
                }
            }
            else
            {
                result = "Данный отряд не принадлежит этому игроку";
            }
         
            return  Json(result, JsonRequestBehavior.AllowGet);
        }

    }
}