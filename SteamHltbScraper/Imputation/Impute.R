#get job data path from environment variable
dataPath = Sys.getenv("WEBJOBS_DATA_PATH");

#redirect output to log file
sink(file.path(dataPath, "R.log"));

#read CSV
ttb = read.csv(file=file.path(dataPath, "ttb.csv"),head=FALSE,sep=",");

#Load imputation library
.libPaths("R/library");
library(robCompositions);

#impute
imputed = impKNNa(ttb)

#Save results as CSV
write.csv(imputed$xImp, file.path(dataPath, "imputed.csv"), row.names=FALSE);