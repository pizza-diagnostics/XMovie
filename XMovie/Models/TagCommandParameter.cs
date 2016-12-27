using XMovie.Models.Data;
using XMovie.ViewModels;

namespace XMovie.Models
{
    public class TagCommandParameter
    {
        private int tagCategoryId
        {
            set { tagCategoryId = value; }
            get { return Tag?.TagCategoryId ?? tagCategoryId; }
        } 
        public int TagCategoryId { get; set; }

        private string name;
        public string Name
        {
            set { name = value; }
            get { return Tag?.Name ?? name; }
        }

        public Tag Tag { get; set; }
    }
}
