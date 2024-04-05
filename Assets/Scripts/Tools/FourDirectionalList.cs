
using System.Collections;
using System.Collections.Generic;

public class FourDirectionalList<T> : IEnumerable<T>
{
    private readonly List<List<T>> topLeft;
    private readonly List<List<T>> topRight;
    private readonly List<List<T>> bottomLeft;
    private readonly List<List<T>> bottomRight;
    private readonly T defaultItem;

    public FourDirectionalList(T defaultItem)
    {
        this.defaultItem = defaultItem;
        topLeft = new List<List<T>>();
        topRight = new List<List<T>>();
        bottomLeft = new List<List<T>>();
        bottomRight = new List<List<T>>();
    }

    public T GetListItem(int xLocation, int yLocation)
    {
        if (xLocation < 0)
        {
            xLocation = -xLocation + 1;
            if (yLocation < 0)
            {
                yLocation = -yLocation + 1;
                return bottomLeft[xLocation][yLocation];
            }

            return topLeft[xLocation][yLocation];
        }

        if (yLocation < 0)
        {
            yLocation = -yLocation + 1;
            return bottomRight[xLocation][yLocation];
        }

        return topRight[xLocation][yLocation];
    }

    private T GetListItem(List<List<T>> targetList, int xLocation, int yLocation)
    {
        return targetList[xLocation][yLocation];
    }

    public bool TryGetListItem(int xLocation, int yLocation, out T item)
    {
        if (xLocation < 0)
        {
            xLocation = -xLocation + 1;
            if (yLocation < 0)
            {
                yLocation = -yLocation + 1;
                return TryGetListItem(bottomLeft, xLocation, yLocation, out item);
            }
            
            return TryGetListItem(topLeft, xLocation, yLocation, out item);
        }

        if (yLocation < 0)
        {
            yLocation = -yLocation + 1;
            return TryGetListItem(bottomRight, xLocation, yLocation, out item);
        }
        
        return TryGetListItem(topRight, xLocation, yLocation, out item);
    }

    public void AddItemToList(T item, int xLocation, int yLocation)
    {
        if (xLocation < 0)
        {
            xLocation = -xLocation + 1;
            if (yLocation < 0)
            {
                yLocation = -yLocation + 1;
                AddItemToList(bottomLeft, item, xLocation, yLocation);
                return;
            }

            AddItemToList(topLeft, item, xLocation, yLocation);
            return;
        }

        if (yLocation < 0)
        {
            yLocation = -yLocation + 1;
            AddItemToList(bottomRight, item, xLocation, yLocation);
            return;
        }

        AddItemToList(topRight, item, xLocation, yLocation);
        return;
    }

    private void AddItemToList(List<List<T>> targetList, T item, int xIndex, int yIndex)
    {
        while (targetList.Count <= xIndex)
            targetList.Add(new List<T>());

        List<T> subList = targetList[xIndex];
        while (subList.Count <= yIndex)
            subList.Add(defaultItem);

        subList[yIndex] = item;
    }

    private bool TryGetListItem(List<List<T>> targetList, int xIndex, int yIndex, out T item)
    {
        if (targetList.Count <= xIndex)
        {
            item = default;
            return false;
        }

        if (targetList[xIndex].Count <= yIndex)
        {
            item = default;
            return false;
        }

        item = targetList[xIndex][yIndex];
        if (defaultItem == null)
            return item != null;

        return item == null || !item.Equals(defaultItem);
    }

    public IEnumerator<T> GetEnumerator()
    {
        foreach (List<T> subList in bottomLeft)
            foreach (T item in subList)
                if (!(item == null && defaultItem == null) && (item != null && !item.Equals(defaultItem)))
                    yield return item;

        foreach (List<T> subList in bottomRight)
            foreach (T item in subList)
                if (!(item == null && defaultItem == null) && (item != null && !item.Equals(defaultItem)))
                    yield return item;

        foreach (List<T> subList in topLeft)
            foreach (T item in subList)
                if (!(item == null && defaultItem == null) && (item != null && !item.Equals(defaultItem)))
                    yield return item;

        foreach (List<T> subList in topRight)
            foreach (T item in subList)
                if (!(item == null && defaultItem == null) && (item != null && !item.Equals(defaultItem)))
                    yield return item;

    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}