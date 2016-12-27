using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XMovie.Models.Data;
using XMovie.Models.Repository;

namespace XMovie.ViewModels
{
    public class SearchTagMenuItemViewModel : ViewModelBase
    {
        public SearchTagMenuItemViewModel()
        {
            MenuItems = new ObservableCollection<SearchTagMenuItemViewModel>();
        }

        private ObservableCollection<SearchTagMenuItemViewModel> menuItems;
        public ObservableCollection<SearchTagMenuItemViewModel> MenuItems
        {
            get { return menuItems; }
            set { SetProperty(ref menuItems, value, "MenuItems"); }
        }

        private string header;
        public string Header
        {
            get { return header; }
            set { SetProperty(ref header, value, "Header"); }
        }

        public Tag Tag { get; set; }

        public bool IsTag
        {
            get { return Tag != null; }
        }

        public bool IsMovieTag { get; set; }

        public static SearchTagMenuItemViewModel CreateTree(string movieId)
        {
            var model = new SearchTagMenuItemViewModel();
            using (var repos = new RepositoryService())
            {
                var categories = repos.GetAllCategories();
                var currentTags = repos.FindMovieTags(new string[] { movieId }).Select(t => t.TagId);

                foreach (var c in categories)
                {
                    var categoryModel = new SearchTagMenuItemViewModel() { Header = c.Name };
                    var tags = repos.FindCategoryTags(c.TagCategoryId);
                    foreach (var t in tags)
                    {
                        var tagModel = new SearchTagMenuItemViewModel() { Header = t.Name, Tag = t };
                        tagModel.IsMovieTag = currentTags.Contains(tagModel.Tag.TagId);
                        categoryModel.MenuItems.Add(tagModel);
                    }
                    model.MenuItems.Add(categoryModel);
                }
            }
            return model;
        }

        public static SearchTagMenuItemViewModel CreateTree()
        {
            var model = new SearchTagMenuItemViewModel();

            using (var repos = new RepositoryService())
            {
                var categories = repos.GetAllCategories();
                foreach (var c in categories)
                {
                    var categoryModel = new SearchTagMenuItemViewModel() { Header = c.Name };
                    var tags = repos.FindCategoryTags(c.TagCategoryId);
                    foreach (var t in tags)
                    {
                        var tagModel = new SearchTagMenuItemViewModel() { Header = t.Name, Tag = t };
                        categoryModel.MenuItems.Add(tagModel);
                    }
                    model.MenuItems.Add(categoryModel);
                }
            }
            return model;
        }
    }
}
