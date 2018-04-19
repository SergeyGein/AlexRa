using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Front.Domain.Abstract;
using Front.Domain.Entites;

namespace Front.Domain.Concrete
{
    public class EFUnitRepository:IUnitRepository
    {
        private EFDbContext context = new EFDbContext();
        public IQueryable<Unit> Units
        {         
           get { return context.Units.Where(a => a.Way < DateTime.Now && a.X >= 0); }
        }
       public void SaveUnit(Unit unit)
        {
            if(unit.UnitID ==0)
            {
                context.Units.Add(unit);

            }
            else 
            {
                Unit un = context.Units.Find(unit.UnitID);
                if(un !=null)
                {
                    un.Ammunition = unit.Ammunition;
                    un.Class = unit.Class;
                    un.HP = unit.HP;
                    un.Land = unit.Land;
                    un.Name = unit.Name;
                    un.PlayerMail = unit.PlayerMail;
                    un.Trench = unit.Trench;
                    un.Turn = unit.Turn;
                    un.Type = unit.Type;
                    un.Way = unit.Way;
                    un.X = unit.X;
                    un.Y = unit.Y;
                }
            }
            context.SaveChanges();
        }
       public void DeleteUnit(int UnitId)
        {
            Unit un = context.Units.Find(UnitId);
            if (un != null)
            {
                context.Units.Remove(un);
                context.SaveChanges();
            }

          }
       public ResultTurn Turn(string PlayerMail, string playerName, int UnitId, int X, int Y)
        {
            ResultTurn mes = new ResultTurn();
            Unit Attacker = context.Units.Find(UnitId);
            if (PlayerMail!=Attacker.PlayerMail)
            {
                mes.Message = "Данный юнит не принадлежит игроку"+ playerName;
                mes.Correct = false;
                return mes;
            }
            else
            {    
                if ((Math.Abs(Attacker.X -X)>1)|| (Math.Abs(Attacker.Y- Y) > 1))
                {
                    mes.Message = "Данный юнит не может произвести перемещение в току, с координатами x: "+X+" y: "+Y+", так как находится на расстоянии большем чем 1 от данной области.";
                    mes.Correct = false;
                    return mes;
                }
                Unit Defence = context.Units.Where(a => a.X == X && a.Y == Y).OrderBy(s => s.HP).FirstOrDefault();
                if(Defence ==null)
                {
                    Attacker.X = X;
                    Attacker.Y = Y;
                    Attacker.Turn = DateTime.Now;
                   // context.SaveChanges();
                    mes.Message = Attacker.Land + " произвёл захват области с координатами x: " + X + " y: " + Y+", захват произвёл игрок "+ playerName;
                    mes.Correct = true;
                    mes.AtackerUnit = Attacker;
                    //Реализовать запись в журнал событий
                    context.Journals.Add(new Journal(PlayerMail, mes.Message, false));
                    context.SaveChanges();
                    return mes;
                }

                if (Defence.Land == Attacker.Land)
                {
                    Attacker.X = X;
                    Attacker.Y = Y;
                    Attacker.Turn = DateTime.Now;                   
                   // context.SaveChanges();
                    mes.Message = Attacker.Land + " произвёл перемещение в область с координатами x: " + X + " y: " + Y + ", игрок " + playerName;
                    mes.Correct = true;
                    mes.AtackerUnit = Attacker;
                    //Реализовать запись в журнал событий
                    context.Journals.Add(new Journal(PlayerMail, mes.Message, false));
                    context.SaveChanges();
                    return mes;
                }
                if (Defence.Land != Attacker.Land)
                {
                    if (Attacker.Ammunition <= 0)
                    {
                        //Если у атакующего нет снарядов
                        mes.Message = "Данный юнит не может произвести атаку, так как у нет боеприпасов!";
                        mes.Correct = false;
                        return mes;
                    }
                    //Происходит бой. Производим расчёт
                    Unit Defencer = context.Units.Where(a => a.X == X && a.Y == Y).OrderBy(b => b.HP).FirstOrDefault();
                    Random rand = new Random();
                    UnitParam AtackParam = context.UnitParams.Where(a => a.Class == Attacker.Class).FirstOrDefault();
                    UnitParam DefenceParam;
                    if (Attacker.Class == Defencer.Class) DefenceParam = AtackParam;
                    else DefenceParam = context.UnitParams.Where(a => a.Class == Defencer.Class).FirstOrDefault();
                    mes.IsAttackMove = true;
                    if (Attacker.Type == "Artillery")
                    {
                        //Расчёт артобстрела
                        switch (Defencer.Type)
                        {
                            case "Infantry": mes.AtackerUron = AtackParam.InfantryAttack - AtackParam.InfantryAttack * (Defencer.Trench / 10) - DefenceParam.ArtilleryDefence; break;
                            case "Artillery": mes.AtackerUron = AtackParam.ArtilleryAttack - AtackParam.ArtilleryAttack * (Defencer.Trench / 10) - DefenceParam.ArtilleryDefence; break;
                            default: mes.AtackerUron = AtackParam.InfantryAttack; break;
                        }
                        mes.AtackerUron += (rand.Next(0, 11) - 5); //Небольшой разбег на удачу для разнообразия
                        mes.AtackerUron = mes.AtackerUron * (Attacker.HP / 100);
                        Attacker.Ammunition -= 1;
                        Attacker.Turn = DateTime.Now;
                        Defencer.HP -= mes.AtackerUron;
                        mes.Message = Attacker.Land + " произвёл артобстрел местности, с координатами x: " + Defencer.X + " , y: " + Defencer.Y + " . В результате осбстрела юнит " + Defencer.Name + " ( " + Defencer.Type + " )" +
                            ", принадлежащий игроку " + Defencer.PlayerMail + " , получил повреждения " + mes.AtackerUron+".";
                        if (Defencer.HP <= 0)
                        {
                            // Защищающийся юнит уничтожен
                            context.Units.Remove(Defencer);
                            mes.Message += " Юнит в результате артобстрела был полностью уничтожен!";
                            mes.DefencyIsRIP = true;
                        }
                        context.Entry(Attacker).State = System.Data.Entity.EntityState.Modified;
                        context.Entry(Defencer).State = System.Data.Entity.EntityState.Modified;                      
                        context.Journals.Add(new Journal(PlayerMail, mes.Message, false));
                        context.SaveChanges();
                        return mes;
                    }
                    else
                    {
                        switch(Defencer.Type)
                        {
                            case "Infantry": mes.AtackerUron = AtackParam.InfantryAttack - AtackParam.InfantryAttack * (Defencer.Trench / 10); break;
                            case "Artillery": mes.AtackerUron = AtackParam.ArtilleryAttack - AtackParam.ArtilleryAttack * (Defencer.Trench / 10); break;
                            default: mes.AtackerUron = AtackParam.InfantryAttack; break;
                        }

                        switch (Attacker.Type)
                        {
                            case "Infantry": mes.AtackerUron -= DefenceParam.InfantryDefence; mes.DefenceUron = DefenceParam.InfantryAttack*(Defencer.HP/100); break;
                            
                            default: mes.AtackerUron = AtackParam.InfantryAttack; mes.DefenceUron = DefenceParam.InfantryAttack; break;
                        }

                        mes.AtackerUron += (rand.Next(0, 11) - 5); //Небольшой разбег на удачу для разнообразия
                        mes.AtackerUron = mes.AtackerUron * (Attacker.HP / 100);
                        Attacker.Ammunition -= 1;
                        Attacker.Turn = DateTime.Now;                     
                        mes.Message = Attacker.Land + " произвёл атаку местности, с координатами x: " + Defencer.X + " , y: " + Defencer.Y + " . В результате атаки юнит " + Defencer.Name + " ( " + Defencer.Type + " )" +
                           ", принадлежащий игроку " + Defencer.PlayerMail + " , получил повреждения " + mes.AtackerUron + ".";
                        if (Defencer.Ammunition>0)
                        {
                            mes.DefenceUron+= (rand.Next(0, 11) - 5);
                            mes.DefenceUron = mes.DefenceUron * (Defencer.HP / 100);
                            Attacker.HP -= mes.DefenceUron;
                            Defencer.Ammunition -= 1;
                            mes.Message += " Атакующий юнит " + Attacker.Name + " ( " + Attacker.Type + " ) получил урон " + mes.DefenceUron;
                        }
                        Defencer.HP -= mes.AtackerUron;
                        if (Defencer.HP <= 0)
                        {
                            // Защищающийся юнит уничтожен
                            context.Units.Remove(Defencer);
                            mes.Message += " Юнит в результате атаки был полностью уничтожен!";
                            Attacker.X = X;
                            Attacker.Y = Y;
                            mes.DefencyIsRIP = true;
                        }
                        if(Attacker.HP<=0)
                        {
                            // Атакующий юнит уничтожен
                            context.Units.Remove(Attacker);
                            mes.Message += " Атакующий в ходе атаки был полностью уничтожен!";
                            mes.AtackerIsRIP = true;
                        }
                        context.Journals.Add(new Journal(PlayerMail, mes.Message, false));
                        context.Entry(Attacker).State = System.Data.Entity.EntityState.Modified;
                        context.Entry(Defencer).State = System.Data.Entity.EntityState.Modified;
                        context.SaveChanges();
                        return mes;


                    }

                    return mes;
                }
            }   

                return mes;
        }

        
       

    }
    
}
