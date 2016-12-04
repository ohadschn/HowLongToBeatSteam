echo "Deleting bin and obj folders..."
gci * -include bin,obj -recurse | remove-item -force -recurse
echo "Done"
Read-Host -Prompt "Press Enter to continue"