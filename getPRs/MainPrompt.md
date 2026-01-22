write an application in .NET
You are given a list of repos in repo_list.json file
Iterate through all the repos

Main Task
Find list of PRs between two commits on GitHub repositories listed in repo_list.json

You will be given two commits, one old and one new. Get the tool to find list of all the commits in between them and get list of corresponding PRs associated with them

Take care of GitHub rate limit issues. Use a token for authentication

Make the calls efficient. Make parallel calls. 

The commits can also exist on azure devops. in that case make calles to azdo instead. Those repo URLs are also listed on the repo_list.json files. Make sure to add tokens for both Azdo and GiHub. In case of Azdo 