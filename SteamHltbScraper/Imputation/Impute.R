print("get job data path from environment variable");
dataPath = Sys.getenv("WEBJOBS_DATA_PATH");

print("redirect output to log file");
sink(file.path(dataPath, "R.log"));

print("read CSV");
ttb = read.csv(file=file.path(dataPath, "ttb.csv"),head=FALSE,sep=",");

print("Load imputation library");
library(robCompositions);

print("impute");
imputed = impKNNa(ttb);

print("Save results as CSV");
write.csv(imputed$xImp, file.path(dataPath, "imputed.csv"), row.names=FALSE);