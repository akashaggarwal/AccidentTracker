//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace DAL
{
    using System;
    using System.Collections.Generic;
    
    public partial class Subscription
    {
        public long Id { get; set; }
        public int ProgramId { get; set; }
        public long SubscriberID { get; set; }
    
        public virtual Subscriber Subscriber { get; set; }
        public virtual AccidentProgram AccidentProgram { get; set; }
    }
}
