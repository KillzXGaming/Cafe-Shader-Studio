using System;
using System.Collections.Generic;
using System.Text;
using Toolbox.Core;

namespace RedStarLibrary
{
    public class InitSubActor
    {
        public List<ActorCreate> Actors = new List<ActorCreate>();

        private dynamic _root;

       public InitSubActor(dynamic root)
        {
            _root = root;

            var rootDict = (IDictionary<string, dynamic>)root;

            foreach (var item in (IList<dynamic>)rootDict["CreatorList"]) {
                Actors.Add(new ActorCreate(item));
            }
        }
    }
}
