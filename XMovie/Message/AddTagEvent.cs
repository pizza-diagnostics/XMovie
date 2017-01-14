using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XMovie.Models.Data;

namespace XMovie.Message
{
    public class AddTagEventItem 
    {
        public Tag Tag { get; set; }
        public object Sender { get; set; }
    }

    public class AddTagEvent : PubSubEvent<AddTagEventItem>
    {

    }
}
