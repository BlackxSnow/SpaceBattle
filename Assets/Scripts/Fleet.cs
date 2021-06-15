using AI;
using Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Management
{
    public enum Designation
    {
        Trade,
        Combat,
        Logistics
    }
    public class Fleet
    {
        public struct MemberData
        {
            public Ship Member;
            public Designation Designation;

            public MemberData(Ship member, Designation designation)
            {
                Member = member;
                Designation = designation;
            }
        }
        public string Name;
        public List<MemberData> Members;

        public StateMachine StrategicAI = new StateMachine();

        public bool RemoveMember(Ship toRemove)
        {
            try
            {
                return Members.Remove(Members.Find(m => m.Member == toRemove));
            }
            catch (ArgumentNullException)
            {
                return false;
            }
        }
        public bool AddMember(Ship toAdd)
        {
            if(Members.Exists(m => m.Member == toAdd))
            {
                return false;
            }
            else
            {
                Members.Add(new MemberData(toAdd, Designation.Combat));
                return true;
            }
        }


        public Fleet(string name, params Ship[] ships)
        {
            Name = name;
            Members = new List<MemberData>(ships.Length);

            //TODO set actual designations
            for(int i = 0; i < Members.Count; i++)
            {
                Members[i] = new MemberData()
                {
                    Member = ships[i],
                    Designation = Designation.Combat
                };
            }
        }
    }
}
