using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XMovie.Models.Data;

namespace XMovie.Message
{
    public class RemoveTagEvent : PubSubEvent<Tag>
    {
    }
}
