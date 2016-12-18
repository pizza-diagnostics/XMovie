using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public void CreateTree()
        {
            using (var repos = new RepositoryService())
            {
                var categories = repos.GetAllCategories();
                foreach (var c in categories)
                {
                    var categoryModel = new SearchTagMenuItemViewModel() { Header = c.Name };
                    var tags = repos.FindCategoryTags(c.TagCategoryId);
                    foreach (var t in tags)
                    {
                        var tagModel = new SearchTagMenuItemViewModel() { Header = t.Name };
                        categoryModel.MenuItems.Add(tagModel);
                    }
                    MenuItems.Add(categoryModel);
                }
            }
        }
    }
}
