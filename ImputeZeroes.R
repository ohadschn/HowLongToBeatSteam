options(echo=TRUE);

print("Reading input...");
ttb = maml.mapInputPort(1);
print(ttb);

print("Removing game names for imputation...")
ttb[1] = NULL

print("Converting zeroes to NAs...");
ttb[ttb == 0] <- NA

print("Loading imputation library...");
library(robCompositions);

print("Imputing...");
imputed = impKNNa(ttb);

print("Imputation complete:");
ximp = imputed$xImp;
print(ximp);

print("Returning result...");
result = data.frame(ximp);
maml.mapOutputPort("result");