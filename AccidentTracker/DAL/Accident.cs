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
    
    public partial class Accident
    {
        public long Id { get; set; }
        public int ProgramID { get; set; }
        public string AccidentID { get; set; }
        public System.DateTime AccidentDate { get; set; }
        public string Name { get; set; }
        public System.DateTime InsertDateTime { get; set; }
    
        public virtual AccidentProgram AccidentProgram { get; set; }
    }
}
