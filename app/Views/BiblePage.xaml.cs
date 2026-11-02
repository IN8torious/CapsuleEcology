using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using CapsuleEcology.Services;

namespace CapsuleEcology.Views
{
    public sealed partial class BiblePage : Page
    {
        private List<BibleBook> _books = new();
        private double _font = 15;
        private bool _wiring;

        public BiblePage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _books = BibleData.Load();
            var (b, c, v) = BibleData.Totals();
            Summary.Text = $"Free for all users — the whole canon on-device, the Toolbox rendering (God, never Yahweh — audited at 0 residual). {b} books · {c} chapters · {v} verses, counted from the data itself. Opens at Psalm 23.";
            _wiring = true;
            BookSel.ItemsSource = _books.Select(x => x.Name).ToList();
            _wiring = false;
            int psalms = _books.FindIndex(x => x.Name == "Psalms");
            BookSel.SelectedIndex = psalms >= 0 ? psalms : 0;
        }

        private void BookSel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_wiring || BookSel.SelectedIndex < 0) return;
            var book = _books[BookSel.SelectedIndex];
            _wiring = true;
            ChapterSel.ItemsSource = book.Chapters.Select(ch => ch.Number.ToString()).ToList();
            _wiring = false;
            // Land on Psalm 23 when Psalms is (re)chosen from the initial load path;
            // otherwise start at chapter 1.
            ChapterSel.SelectedIndex = book.Name == "Psalms" && ChapterSel.Items.Count >= 23 && VerseText.Text.Length == 0 ? 22 : 0;
        }

        private void ChapterSel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_wiring || BookSel.SelectedIndex < 0 || ChapterSel.SelectedIndex < 0) return;
            ShowChapter(BookSel.SelectedIndex, ChapterSel.SelectedIndex);
        }

        private void ShowChapter(int bi, int ci)
        {
            var book = _books[bi];
            var chapter = book.Chapters[ci];
            var sb = new StringBuilder();
            sb.AppendLine($"{book.Name} {chapter.Number}");
            sb.AppendLine();
            for (int i = 0; i < chapter.Verses.Count; i++)
                sb.AppendLine($"{i + 1}  {chapter.Verses[i]}");
            VerseText.FontSize = _font;
            VerseText.Text = sb.ToString();
            HitList.Visibility = Visibility.Collapsed;
            ReaderScroll.Visibility = Visibility.Visible;
            ReaderScroll.ChangeView(null, 0, null, true);
        }

        private void BtnFontM_Click(object sender, RoutedEventArgs e) { _font = Math.Max(11, _font - 1); VerseText.FontSize = _font; }
        private void BtnFontP_Click(object sender, RoutedEventArgs e) { _font = Math.Min(22, _font + 1); VerseText.FontSize = _font; }

        private void BtnSearch_Click(object sender, RoutedEventArgs e) => RunSearch();
        private void SearchBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter) RunSearch();
        }

        private void RunSearch()
        {
            var hits = BibleData.Search(SearchBox.Text.Trim());
            HitList.ItemsSource = hits;
            HitList.Visibility = Visibility.Visible;
            ReaderScroll.Visibility = Visibility.Collapsed;
            Summary.Text = hits.Count == 0
                ? $"No verses contain “{SearchBox.Text.Trim()}”."
                : $"{hits.Count} verse(s) contain “{SearchBox.Text.Trim()}”{(hits.Count >= 200 ? " (first 200 shown)" : "")} — select one to read it in place.";
        }

        private void HitList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (HitList.SelectedItem is not BibleHit hit) return;
            _wiring = true;
            BookSel.SelectedIndex = hit.BookIndex;
            var book = _books[hit.BookIndex];
            ChapterSel.ItemsSource = book.Chapters.Select(ch => ch.Number.ToString()).ToList();
            ChapterSel.SelectedIndex = hit.ChapterIndex;
            _wiring = false;
            ShowChapter(hit.BookIndex, hit.ChapterIndex);
        }
    }
}
