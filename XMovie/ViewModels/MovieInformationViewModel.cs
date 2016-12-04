using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using XMovie.Common;
using XMovie.Models;

namespace XMovie.ViewModels
{
    public class MovieInformationViewModel : ViewModelBase
    {
        public MovieInformationViewModel()
        {
            Tags = new ObservableCollection<TagViewModel>();
            using (var context = new XMovieContext())
            {
                foreach (var category in context.TagCategories)
                {
                    var tag = new TagViewModel()
                    {
                        TagCategoryId = category.TagCategoryId,
                        CategoryName = category.Name
                    };
                    Tags.Add(tag);
                }
            }
        }

        private ObservableCollection<object> selectedMovies;
        public ObservableCollection<object> SelectedMovies
        {
            get { return selectedMovies; }
            set {
                selectedMovies = value;
                foreach (var tagViewModel in Tags)
                {
                    tagViewModel.SelectedMovies = value;
                }
            }
        }

        #region New Category
        private string newCategoryName;
        public string NewCategoryName
        {
            get { return newCategoryName; }
            set
            {
                if (SetProperty(ref newCategoryName, value))
                {
                    OnPropertyChanged("EnableAddCategory");
                }
            }
        }

        public bool EnableAddCategory
        {
            get { return NewCategoryName != null && NewCategoryName.Length > 0; }
        }

        private ICommand addNewCategoryCommand;
        public ICommand AddNewCategoryCommand
        {
            get
            {
                if (addNewCategoryCommand == null)
                {
                    addNewCategoryCommand = new RelayCommand((param) =>
                    {
                        string categoryName = (string)param;
                        using (var context = new XMovieContext())
                        {
                            var isExist = context.TagCategories.Any(c => c.Name.Equals(categoryName));
                            if (isExist)
                            {
                                SetError("AddnewCategoryCommand", $"{categoryName}は登録済みです。");
                            }
                            else
                            {
                                var category = new TagCategory() { Name = categoryName };
                                context.TagCategories.Add(category);
                                context.SaveChanges();
                                Tags.Add(new TagViewModel() {
                                    TagCategoryId = category.TagCategoryId,
                                    CategoryName = categoryName
                                });

                            }
                            NewCategoryName = "";
                        }
                    });
                }
                return addNewCategoryCommand;
            }
        }

        #endregion

        #region Tag
        private ObservableCollection<TagViewModel> tags;
        public ObservableCollection<TagViewModel> Tags
        {
            get { return tags; }
            set { tags = value; }
        }

        #endregion
    }
}
