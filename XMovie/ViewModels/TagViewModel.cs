using Prism.Mvvm;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using XMovie.Common;
using XMovie.Models.Data;
using XMovie.Models.Repository;
using XMovie.Service;

namespace XMovie.ViewModels
{
    public class TagViewModel : BindableBase
    {
        private IDialogService dialogService;

        public TagViewModel(IDialogService dialogService)
        {
            this.dialogService = dialogService;
        }

        #region Category
        private string categoryName;
        public string CategoryName
        {
            get { return this.categoryName; }
            set { SetProperty(ref categoryName, value, "CategoryName"); }
        }

        private int tagCategoryId;
        public int TagCategoryId
        {
            get { return tagCategoryId; }
            set
            {
                tagCategoryId = value;
                UpdateCategoryTags();
            }
        }

        #endregion

        #region Tags

        public void UpdateCategoryTags()
        {
            using (var repo = new RepositoryService())
            {
                var tags = repo.FindCategoryTags(TagCategoryId).Select(t => t.Name);
                CategoryTags = new ObservableCollection<string>(tags);
            }
        }

        private void UpdateSelectedMovieTags()
        {
            // 選択されている動画に設定されているタグをTagsに設定
            var selectedMovieIdList = selectedMovies?.Select(m => m.MovieId);
            if (selectedMovieIdList == null)
            {
                Tags = new ObservableCollection<Tag>();
            }
            else
            {
                using (var repos = new RepositoryService())
                {
                    Tags = new ObservableCollection<Tag>(repos.FindMovieTags(selectedMovieIdList.ToList(), TagCategoryId));
                }
            }
        }

        private ObservableCollection<Tag> tags;
        public ObservableCollection<Tag> Tags
        {
            get { return tags; }
            set { SetProperty(ref tags, value, "Tags"); }
        }

        private ObservableCollection<string> categoryTags;
        public ObservableCollection<string> CategoryTags
        {
            get { return categoryTags; }
            set { SetProperty(ref categoryTags, value, "CategoryTags"); }

        }

        private bool enableTagEdit = false;
        public bool EnableTagEdit
        {
            get { return enableTagEdit; }
            set { SetProperty(ref enableTagEdit, value, "EnableTagEdit"); }
        }

        private string addTagText;
        public string AddTagText
        {
            get { return addTagText; }
            set { SetProperty(ref addTagText, value, "AddTagText"); }
        }

        private ICommand addTagCommand;
        public ICommand AddTagCommand
        {
            get
            {
                return addTagCommand ?? (addTagCommand = new RelayCommand((param) =>
                {
                    var tag = (Tag)param;
                    var targetMovieIdList = SelectedMovies.Select(m => m.MovieId);

                    using (var repos = new RepositoryService())
                    {
                        repos.SetTagToMovies(tag, targetMovieIdList.ToList());
                    }

                    UpdateSelectedMovieTags();
                    UpdateCategoryTags();
                    AddTagText = ""; // inputのクリア 
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
                    var tagId = ((Tag)param).TagId;
                    var movieIdList = selectedMovies.Select(m => m.MovieId).ToList();

                    using (var repo = new RepositoryService())
                    {
                        repo.RemoveTagMaps(tagId, movieIdList);
                    }

                    UpdateSelectedMovieTags();
                    UpdateCategoryTags();
                }));
            }
        }

        private ICommand removeCategoryCommand;
        public ICommand RemoveCategoryCommand
        {
            get
            {
                return removeCategoryCommand ?? (removeCategoryCommand = new RelayCommand(async (param) =>
                {
                    var result = await this.dialogService.ShowConfirmDialog("foo", "bar");
                }));
            }
        }
        #endregion

        #region SelectedMovies

        private ObservableCollection<MovieItemViewModel> selectedMovies;
        public ObservableCollection<MovieItemViewModel> SelectedMovies
        {
            get { return selectedMovies; }
            set
            {
                selectedMovies = value;
                EnableTagEdit = selectedMovies?.Count > 0;
                UpdateSelectedMovieTags();
            }
        }

        #endregion
    }
}
