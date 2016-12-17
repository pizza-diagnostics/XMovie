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
using XMovie.Models.Repository;
using XMovie.Service;

namespace XMovie.ViewModels
{
    public class MovieInformationViewModel : ViewModelBase
    {
        private IDialogService dialogService;

        public MovieInformationViewModel(IDialogService dialogService)
        {
            this.dialogService = dialogService;

            Tags = new ObservableCollection<TagViewModel>();
            using (var repos = new RepositoryService())
            {
                foreach (var category in repos.GetAllCategories())
                {
                    var tagModel = new TagViewModel(dialogService)
                    {
                        TagCategoryId = category.TagCategoryId,
                        CategoryName = category.Name
                    };
                    Tags.Add(tagModel);
                }
            }
        }

        private ObservableCollection<MovieItemViewModel> selectedMovies;
        public ObservableCollection<MovieItemViewModel> SelectedMovies
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
                        using (var repos = new RepositoryService())
                        {
                            if (repos.IsExistCategory(categoryName))
                            {
                                SetError("AddnewCategoryCommand", $"{categoryName}は登録済みです。");
                            }
                            else
                            {
                                var category = repos.InsertNewCategory(categoryName);

                                Tags.Add(new TagViewModel(dialogService)
                                {
                                    TagCategoryId = category.TagCategoryId,
                                    CategoryName = category.Name
                                });
                            }
                            NewCategoryName = "";
                        }
                        SelectedMovies = selectedMovies;
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

        private ICommand addTagCommand;
        public ICommand AddTagCommand
        {
            get
            {
                if (addTagCommand == null)
                {
                    addTagCommand = new RelayCommand((param) =>
                    {
                        var tagParam = (Tag)param;
                        foreach (TagViewModel tag in Tags)
                        {
                            if (tag.TagCategoryId == tagParam.TagCategoryId)
                            {
                                tag.AddTagCommand.Execute(param);
                            }
                        }
                    });
                }
                return addTagCommand;
            }
        }

        private ICommand removeTagCommand;
        public ICommand RemoveTagCommand
        {
            get
            {
                if (removeTagCommand == null)
                {
                    removeTagCommand = new RelayCommand((param) =>
                    {
                        foreach (TagViewModel tag in Tags)
                        {
                            tag.RemoveTagCommand.Execute(param);
                        }
                    });
                }
                return removeTagCommand;
            }
        }

        private ICommand removeCategoryCommand;
        public ICommand RemoveCategoryCommand
        {
            get
            {
                if (removeCategoryCommand == null)
                {
                    removeCategoryCommand = new RelayCommand((param) =>
                    {
                        var tagViewModel = (TagViewModel)param;
                        var categoryId = tagViewModel.TagCategoryId;

                        using (var repos = new RepositoryService())
                        {
                            repos.RemoveTagCategory(categoryId);
                        }

                        Tags.Remove(Tags.Where(t => t.TagCategoryId == categoryId).FirstOrDefault());

                    });
                }
                return removeCategoryCommand;
            }
        }

        #endregion
    }
}
