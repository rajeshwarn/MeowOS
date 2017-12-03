﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeowOS.ProcScheduler
{
    public class Process
    {
        public enum Priorities { LOW = 0, NORMAL = 1, HIGH = 2, ABSOLUTE = 3 }
        public enum States { UNBORN, BORN, READY, RUNNING, WAITING, COMPLETED, KILLED }

        private int pid;
        public int PID => pid;

        private Priorities priority;
        public Priorities Priority
        {
            get => priority;
            set => priority = value;
        }

        private States state;
        public States State
        {
            get => state;
            set => state = value;
        }
        public bool IsAlive => state == States.BORN || state == States.READY || state == States.RUNNING || state == States.WAITING;

        private int burst;
        public int Burst
        {
            get => burst;
            set => burst = value;
        }

        private int bornTime;
        public int BornTime
        {
            get => bornTime;
            set => bornTime = value;
        }

        private int memRequired;
        public int MemRequired => memRequired;

        public Process()
        {
            state = States.UNBORN;
        }

        public Process(int pid, Priorities priority, int burst, int memRequired, int bornTime) : this()
        {
            this.pid = pid;
            this.priority = priority;
            this.burst = burst;
            this.memRequired = memRequired;
            this.bornTime = bornTime;
        }

        public override string ToString()
        {
            return pid.ToString() + " (" + priority + ")";
        }
    }
}
