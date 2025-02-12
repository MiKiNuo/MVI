﻿namespace MVI.Demo
{
    public record MviResult :IMviResult
    {
        public int Code { set; get; }
        public string Message { set; get; }
        public object Data { set; get; }
    }
}