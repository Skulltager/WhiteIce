
using System;

public class Task
{
    public readonly Action taskToPerform;
    public readonly int priority;
    public readonly int taskID;

    public Task(Action taskToPerform, int priority, int taskID)
    {
        this.taskToPerform = taskToPerform;
        this.priority = priority;
        this.taskID = taskID;
    }
}