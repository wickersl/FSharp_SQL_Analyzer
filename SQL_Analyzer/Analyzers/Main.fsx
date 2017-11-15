#load "..\SQL_Typifier.fsx" //DON'T TOUCH

//Load your analyzers here
#load "Compound_Data.fsx"
#load "Duplicate_Col_Names.fsx"
#load "Too_Many_Nulls.fsx"

//Include your loaded analyzers in this list, with a call to getFinal()
//Don't change the function name
let getAllResults() = [
        Compound_Data.getFinal();
        Duplicate_Col_Names.getFinal();
        Too_Many_Nulls.getFinal()
    ]