using Prism.Events;
using XMovie.ViewModels;

namespace XMovie.Message
{
    public class RemoveMovieEvent : PubSubEvent<MovieItemViewModel>
    {
    }
}
