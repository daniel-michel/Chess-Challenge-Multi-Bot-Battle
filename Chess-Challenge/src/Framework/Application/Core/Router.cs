using System.Collections.Generic;

namespace ChessChallenge.Application
{
    abstract class PageRoute
    {
        public abstract void Show();
    }

    class Router
    {
        Dictionary<string, PageRoute> pages = new();
        Stack<string> pageHistory = new();

        public void AddPage(string name, PageRoute page)
        {
            pages.Add(name, page);
        }

        public void GoToPage(string name)
        {
            pageHistory.Push(name);
        }

        public void GoBack(int numPages = 1)
        {
            for (int i = 0; i < numPages; i++)
            {
                pageHistory.Pop();
            }
        }

        public PageRoute GetCurrentPage()
        {
            return pages[pageHistory.Peek()];
        }
    }
}