using Prism.Events;
using XMovie.ViewModels;

namespace XMovie.Message
{
    public class MoveMovieEvent : PubSubEvent<MovieItemViewModel>
    {
    }
}
