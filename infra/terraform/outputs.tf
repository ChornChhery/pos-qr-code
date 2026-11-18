output "vpc_id" {
  value = aws_vpc.main.id
}

output "ecr_repository_urls" {
  value = { for name, repo in aws_ecr_repository.service : name => repo.repository_url }
}

output "db_endpoint" {
  value = aws_db_instance.main.address
}

output "db_secret_arn" {
  description = "Secrets Manager secret holding the database admin credentials."
  value       = aws_secretsmanager_secret.db.arn
}

output "uploads_bucket" {
  value = aws_s3_bucket.uploads.bucket
}
