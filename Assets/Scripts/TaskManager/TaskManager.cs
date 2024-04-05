using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class TaskManager : MonoBehaviour
{
    public static TaskManager instance { private set; get; }
    [SerializeField] private float miliSecondsPerRun = default;
    [SerializeField] private float miliSecondsDelayPerRun = default;

    private readonly List<Task> tasks;
    private int currentTaskID;
    private Coroutine routine_DoTasks;

    private TaskManager()
    {
        tasks = new List<Task>();
    }

    private void Awake()
    {
        instance = this;
    }

    public int AddTask(Action taskToPerform, int priority)
    {
        int taskID = currentTaskID++;
        Task task = new Task(taskToPerform, priority, taskID);
        tasks.Add(task);

        if(routine_DoTasks == null)
            routine_DoTasks = StartCoroutine(Routine_DoTasks());

        return taskID;
    }

    public void CancelTask(int taskID)
    {
        int index = tasks.FindIndex(i => i.taskID == taskID);
        if (index == -1)
            return;

        tasks.RemoveAt(index);
    }

    private IEnumerator Routine_DoTasks()
    {
        Stopwatch stopwatch = new Stopwatch();
        yield return null;
        while (tasks.Count > 0)
        {
            stopwatch.Start();
            while(stopwatch.Elapsed.TotalMilliseconds < miliSecondsPerRun && tasks.Count > 0)
            {
                tasks.Sort(TaskComparer);
                Task task = tasks[0];
                tasks.RemoveAt(0);
                task.taskToPerform();
            }
            stopwatch.Stop();
            stopwatch.Reset();
            yield return new WaitForSecondsRealtime(miliSecondsDelayPerRun / 1000);
        }
        routine_DoTasks = null;
    }

    private int TaskComparer(Task a, Task b)
    {
        return a.priority.CompareTo(b.priority);
    }
}