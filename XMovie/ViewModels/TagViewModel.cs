using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using XMovie.Common;
using XMovie.Models;
using XMovie.Service;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.Controls;


namespace XMovie.ViewModels
{
    public class TagViewModel : ViewModelBase
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

        public int TagCategoryId { get; set; }

        #endregion

        #region Tags

        private void UpdateCategoryTags()
        {
            using (var context = new XMovieContext())
            {
                var tags = context.Tags.Where(t => t.TagCategoryId == TagCategoryId).Select(t => t.Name).ToList();
                CategoryTags = new ObservableCollection<string>(tags);
            }

        }

        private void UpdateSelectedMovieTags()
        {
            // 選択されている動画に設定されているタグをTagsに設定
            using (var context = new XMovieContext())
            {
                var selectedMovieIdList = selectedMovies?.Select(m => $"'{((MovieItemViewModel)m).MovieId}'");
                if (selectedMovieIdList == null)
                {
                    Tags = new ObservableCollection<Tag>();
                }
                else
                {
                    /*
                    var tags = from tm in context.TagMaps
                               join t in context.Tags
                               on tm.TagId equals t.TagId
                               where selectedMovieIdList.Contains(tm.MovieId) && t.TagCategoryId == TagCategoryId
                               select new Tag() { Name = t.Name, TagId = t.TagId, TagCategoryId = t.TagCategoryId };
                    Tags = new ObservableCollection<Tag>(tags.ToList());
                    */
                    // 妥協
                    var query = $@"
select
    T.*
from
    Tags T, TagMaps TM
where
    T.TagId = TM.TagId and
    T.TagCategoryId = {TagCategoryId} and
    TM.MovieId in ({String.Join(",", selectedMovieIdList)})
group by
    T.TagId
";
                    var results = context.Database.SqlQuery<Tag>(query);
                    Tags = new ObservableCollection<Tag>(results);
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
                if (addTagCommand == null)
                {
                    addTagCommand = new RelayCommand((param) =>
                    {
                        var tag = (Tag)param;
                        using (var context = new XMovieContext())
                        {
                            // 2. 同じタグが設定されていない動画にタグを追加

                            // TagMapから既に同タグが設定されている動画を抽出
                            var targetMovieIdList = SelectedMovies.Select(m => ((MovieItemViewModel)m).MovieId);
                            var ignores = context.TagMaps
                                                 .Where(tm => tm.TagId == tag.TagId && targetMovieIdList.Contains(tm.MovieId))
                                                 .Select(tm => tm.MovieId)
                                                 .ToList();
                            
                            // ignoresに含まれていない動画にのみタグを設定
                            foreach (MovieItemViewModel movie in selectedMovies)
                            {
                                if (!ignores.Contains(movie.MovieId))
                                {
                                    var tagMap = new TagMap() {
                                        TagId = tag.TagId,
                                        MovieId = movie.MovieId
                                    };
                                    context.TagMaps.Add(tagMap);
                                }
                            }
                            context.SaveChanges();
                        }

                        UpdateSelectedMovieTags();
                        UpdateCategoryTags();
                        AddTagText = ""; // inputのクリア 
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
                        var tagId = ((Tag)param).TagId;
                        using (var context = new XMovieContext())
                        {
                            var movieIdList = selectedMovies.Select(m => ((MovieItemViewModel)m).MovieId);
                            var removes = context.TagMaps.Where(tm => movieIdList.Contains(tm.MovieId) && tm.TagId == tagId).Select(tm => tm);
                            context.TagMaps.RemoveRange(removes);
                            context.SaveChanges();
                        }
                        UpdateSelectedMovieTags();
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
                    removeCategoryCommand = new RelayCommand(async (param) =>
                    {
                        var result = await this.dialogService.ShowConfirmDialog("foo", "bar");
                    });
                }
                return removeCategoryCommand;
            }
        }
        #endregion

        #region SelectedMovies

        private ObservableCollection<object> selectedMovies;
        public ObservableCollection<object> SelectedMovies
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
