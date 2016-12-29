using Prism.Mvvm;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using XMovie.Common;
using XMovie.Models.Data;
using XMovie.Models.Repository;
using XMovie.Service;

namespace XMovie.ViewModels
{
    public class MovieInformationViewModel : BindableBase
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
                return addNewCategoryCommand ?? (addNewCategoryCommand = new RelayCommand((param) =>
                {
                    string categoryName = (string)param;
                    using (var repos = new RepositoryService())
                    {
                        if (!repos.IsExistCategory(categoryName))
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
                }));
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
                return addTagCommand ?? (addTagCommand = new RelayCommand((param) =>
                {
                    var tagParam = (Tag)param;
                    foreach (TagViewModel tag in Tags)
                    {
                        if (tag.TagCategoryId == tagParam.TagCategoryId)
                        {
                            tag.AddTagCommand.Execute(param);
                        }
                    }
                }));
            }
        }

        private ICommand removeTagCommand;
        public ICommand RemoveTagCommand
        {
            get
            {
                return removeTagCommand ?? (removeTagCommand = new RelayCommand((param) =>
                {
                    foreach (TagViewModel tag in Tags)
                    {
                        tag.RemoveTagCommand.Execute(param);
                    }
                }));
            }
        }

        private ICommand removeCategoryCommand;
        public ICommand RemoveCategoryCommand
        {
            get
            {
                return removeCategoryCommand ?? (removeCategoryCommand = new RelayCommand((param) =>
                {
                    var tagViewModel = (TagViewModel)param;
                    var categoryId = tagViewModel.TagCategoryId;

                    using (var repos = new RepositoryService())
                    {
                        repos.RemoveTagCategory(categoryId);
                    }

                    Tags.Remove(Tags.Where(t => t.TagCategoryId == categoryId).FirstOrDefault());

                }));
            }
        }

        #endregion
    }
}
