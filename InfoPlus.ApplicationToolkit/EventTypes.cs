using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InfoPlus.ApplicationToolkit.Entities
{

    [Flags]
    public enum EventTypes
    {
        INSTANCE_STARTING = 0x01 << 0,
        INSTANCE_STARTED = 0x01 << 1,
        INSTANCE_COMPLETING = 0x01 << 2,    // could not be retreived from workflow yet.
        INSTANCE_COMPLETED = 0x01 << 3,     // 
        ACTION_DOING = 0x01 << 4,
        ACTION_DONE = 0x01 << 5,
        ACTION_SAVING = 0x01 << 6,          // Save as Draft
        ACTION_SAVED = 0x01 << 7,
        STEP_EXPIRING = 0x01 << 8,
        STEP_EXPIRED = 0x01 << 9,
        FIELD_CHANGING = 0x01 << 10,
        ECHO = 0x01 << 11,

        FIELD_SUGGESTING = 0x01 << 13,       // On external CodeTable need to be suggested.
        STEP_RENDERING = 0x01 << 14,         // Before page rendering, while loading.
        STEP_RENDERED = 0x01 << 15,          // After page rendered, called via Ajax
        ACTION_CLICKING = 0x01 << 16,        // After user clicked an ActionButton, but before select next step(s).
        STEP_PRINTING = 0x01 << 17,          // The same time as STEP_RENDERING, but in print page.
        INSTANCE_EXPIRING = 0x01 << 18,
        INSTANCE_EXPIRED = 0x01 << 19,
        INSTANCE_KILLING = 0x01 << 20,
        INSTANCE_KILLED = 0x01 << 21,

        INSTANCE_COMPENSATION = 0x01 << 22,

        ACTION_WITHDRAWING = 0x01 << 23,
        ACTION_WITHDRAWN = 0x01 << 24,
        INSTANCE_SAVING = 0x01 << 25,
        INSTANCE_SAVED = 0x01 << 26,

        INSTANCE_PRINTNG = 0x01 << 27,
        STEP_EXPORING = 0x01 << 28,
        INSTANCE_EXPORING = 0x01 << 29


    }
}
