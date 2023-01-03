const { Octokit } = require("@octokit/action");

const [owner, repo] = process.env.GITHUB_REPOSITORY.split("/");
const releaseVersion = process.env.RELEASE_VERSION;
const formattedTime = process.env.FORMATTED_TIME;
const token = process.env.GITHUB_TOKEN;

console.log('Owner and repo: {owner} | {repo}')
console.log('Release version: {releaseVersion}')

const octokit = new Octokit({
    auth: '{token}'
});

await octokit.request('POST /repos/{owner}/{repo}/releases', {
  owner: '{owner}',
  repo: '{repo}',
  tag_name: '{releaseVersion}',
  name: '{releaseVersion}',
  body: 'Release version on {formattedTime}',
  target_commitish: 'main',
  draft: false,
  prerelease: false,
  generate_release_notes: false
})
